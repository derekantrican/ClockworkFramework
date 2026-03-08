namespace ClockworkFramework.Core
{
    public enum TimeType
    {
        Second,
        Minute,
        Hour,
        Day,
        Week,
        Month,
        Year,
    }

    public class Interval
    {
        //Todo: simplify interval constructors somehow (and therefore: attribute)
        //Todo: allow for "Interval.Once" to only run a task one time after startup

        public Interval(TimeType timeType, int frequency, int hour, int minute, string timezone = null)
        {
            if (timeType == TimeType.Week)
            {
                throw new ArgumentException("For TimeType.Week, use the constructor Interval(dayOfWeek, frequency, hour, minute)");
            }
            else if (timeType == TimeType.Second || timeType == TimeType.Minute || timeType == TimeType.Hour)
            {
                throw new ArgumentException("For TimeType.Second or TimeType.Minute or TimeType.Hour, use the constructor Interval(timeType, frequency)");
            }
            else if (timeType == TimeType.Month)
            {
                throw new NotSupportedException("TimeType.Month is not currently supported"); //Todo: make another constructor for something like (int dayOfMonth, int hour, int minute)
            }

            TimeType = timeType;
            Frequency = frequency;
            Hour = hour;
            Minute = minute;
            Timezone = timezone;
        }

        public Interval(TimeType timeType, int frequency)
        {
            if (timeType == TimeType.Day || timeType == TimeType.Year)
            {
                throw new ArgumentException("For TimeType.Day or TimeType.Year, use the constructor Interval(timeType, frequency, hour, minute)"); //Todo: specifying hour & minute should be optional, not required
            }
            else if (timeType == TimeType.Week)
            {
                throw new ArgumentException("For TimeType.Week, use the constructor Interval(dayOfWeek, frequency, hour, minute)");
            }
            else if (timeType == TimeType.Month)
            {
                throw new NotSupportedException("TimeType.Month is not currently supported"); //Todo: make another constructor for something like (int dayOfMonth, int hour, int minute)
            }

            TimeType = timeType;
            Frequency = frequency;
        }

        public Interval(DayOfWeek dayOfWeek, int frequency, int hour, int minute, string timezone = null)
        {
            TimeType = TimeType.Week;
            DayOfWeek = dayOfWeek;
            Frequency = frequency;
            Hour = hour;
            Minute = minute;
            Timezone = timezone;
        }

        public TimeType TimeType { get; }
        public int Frequency { get; }
        public DayOfWeek DayOfWeek { get; }
        public int Hour { get; }
        public int Minute { get; }
        public string Timezone { get; }

        public TimeSpan CalculateTimeToNext(DateTime fromDateTime)
        {
            // For relative intervals (Second/Minute/Hour), do arithmetic in UTC to completely
            // avoid DST issues. These intervals don't care about wall-clock time.
            if (TimeType == TimeType.Second || TimeType == TimeType.Minute || TimeType == TimeType.Hour)
            {
                DateTime fromUtc = fromDateTime.Kind == DateTimeKind.Utc
                    ? fromDateTime
                    : fromDateTime.ToUniversalTime();

                DateTime nextUtc = TimeType switch
                {
                    TimeType.Second => fromUtc.AddSeconds(Frequency),
                    TimeType.Minute => fromUtc.AddMinutes(Frequency),
                    TimeType.Hour => fromUtc.AddHours(Frequency),
                    _ => throw new Exception("Unexpected TimeType"),
                };

                return nextUtc - fromUtc;
            }

            // For absolute intervals (Day/Week/Year), we need to calculate in local time
            // (or the specified timezone) since the user wants "run at HH:MM local time".
            TimeZoneInfo tz;
            if (!string.IsNullOrEmpty(Timezone))
            {
                try
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById(Timezone); //I could use https://github.com/mattjohnsonpint/TimeZoneConverter to support IANA timezones
                }
                catch (TimeZoneNotFoundException)
                {
                    throw new Exception($"Unrecognized timezone '{Timezone}'");
                }
            }
            else
            {
                tz = TimeZoneInfo.Local;
            }

            // Convert fromDateTime to the target timezone for accurate wall-clock calculations
            DateTime fromUtcAbs = fromDateTime.Kind == DateTimeKind.Utc
                ? fromDateTime
                : fromDateTime.ToUniversalTime();
            DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(fromUtcAbs, tz);

            DateTime? nextLocal = null;
            switch (TimeType)
            {
                case TimeType.Day:
                    nextLocal = new DateTime(localNow.Year, localNow.Month, localNow.Day, Hour, Minute, 0, 0);

                    if (Frequency != 1 || nextLocal <= localNow) //Only shift forward if the task repeats less frequently than everyday or the time has already passed
                    {
                        nextLocal = nextLocal.Value.AddDays(Frequency);
                    }

                    break;
                case TimeType.Week:
                    nextLocal = new DateTime(localNow.Year, localNow.Month, localNow.Day, Hour, Minute, 0, 0);

                    if (localNow.DayOfWeek != DayOfWeek || nextLocal <= localNow) //Only shift forward if the DayOfWeek occurrence is not today or the time has already passed
                    {
                        int daysUntilTarget = ((int)DayOfWeek - (int)localNow.DayOfWeek + 7) % 7;
                        if (daysUntilTarget == 0) daysUntilTarget = 7; // If same day but time passed, go to next week
                        nextLocal = new DateTime(localNow.Year, localNow.Month, localNow.Day, Hour, Minute, 0, 0).AddDays(daysUntilTarget);

                        if (Frequency > 1)
                        {
                            nextLocal = nextLocal.Value.AddDays(7 * (Frequency - 1));
                        }
                    }

                    break;
                case TimeType.Month:
                    //Todo: not currently supported
                    break;
                case TimeType.Year:
                    nextLocal = new DateTime(localNow.Year, localNow.Month, localNow.Day, Hour, Minute, 0, 0);

                    if (nextLocal <= localNow) //Only shift forward if the time has already passed
                    {
                        nextLocal = nextLocal.Value.AddYears(Frequency);
                    }

                    break;
            }

            if (!nextLocal.HasValue)
            {
                throw new Exception("Unable to calculate next occurence");
            }

            // Handle DST edge cases before converting to UTC:
            // - "Invalid" times fall in the spring-forward gap (e.g. 2:30 AM when clocks skip 2:00->3:00)
            // - "Ambiguous" times occur during fall-back (e.g. 1:30 AM happens twice)
            if (tz.IsInvalidTime(nextLocal.Value))
            {
                // The target time doesn't exist — shift forward by the DST adjustment amount
                // so we land just after the gap (e.g. 2:30 AM becomes 3:30 AM)
                TimeSpan dstDelta = tz.GetAdjustmentRules()
                    .Where(r => r.DateStart <= nextLocal.Value && r.DateEnd >= nextLocal.Value)
                    .Select(r => r.DaylightDelta)
                    .FirstOrDefault();

                if (dstDelta == TimeSpan.Zero)
                    dstDelta = TimeSpan.FromHours(1); // Fallback: most DST transitions are 1 hour

                nextLocal = nextLocal.Value.Add(dstDelta);
            }

            // For ambiguous times (fall-back), ConvertTimeToUtc defaults to standard time offset.
            // This is acceptable — the task will run during the second occurrence of that wall-clock
            // time, which is a reasonable behavior for a scheduler.

            // Convert both times to UTC for an accurate TimeSpan regardless of DST transitions
            DateTime nextUtcAbs = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(nextLocal.Value, DateTimeKind.Unspecified), tz);

            return nextUtcAbs - fromUtcAbs;
        }
    }
}