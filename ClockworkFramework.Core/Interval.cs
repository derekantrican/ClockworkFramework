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
            DateTime? next = null;
            switch (TimeType)
            {
                case TimeType.Second:
                    next = fromDateTime.AddSeconds(Frequency);
                    break;
                case TimeType.Minute:
                    next = fromDateTime.AddMinutes(Frequency);
                    break;
                case TimeType.Hour:
                    next = fromDateTime.AddHours(Frequency);
                    break;
                case TimeType.Day:
                    next = fromDateTime;
                    next = new DateTime(next.Value.Year, next.Value.Month, next.Value.Day, Hour, Minute, 0, 0);

                    if (Frequency != 1 || next < fromDateTime) //Only shift forward if the task repeats less frequently than everyday or the time has already passed
                    {
                        next = next.Value.AddDays(Frequency);
                    }

                    break;
                case TimeType.Week:
                    next = fromDateTime;
                    next = new DateTime(next.Value.Year, next.Value.Month, next.Value.Day, Hour, Minute, 0, 0);

                    if (fromDateTime.DayOfWeek != DayOfWeek || next < fromDateTime) //Only shift forward if the DayOfWeek occurrence is not today or the time has already passed
                    {
                        next = fromDateTime.AddDays(((int)DayOfWeek - (int)fromDateTime.DayOfWeek + 7) % 7);

                        if (Frequency > 1)
                        {
                            next = next.Value.AddDays(7 * Frequency);
                        }
                    }

                    break;
                case TimeType.Month:
                    //Todo: not currently supported
                    break;
                case TimeType.Year:
                    next = fromDateTime;
                    next = new DateTime(next.Value.Year, next.Value.Month, next.Value.Day, Hour, Minute, 0, 0);

                    if (next < fromDateTime) //Only shift forward if the time has already passed
                    {
                        next = fromDateTime.AddYears(Frequency);
                    }

                    break;
            }

            if (!next.HasValue)
            {
                throw new Exception("Unable to calculate next occurence");
            }

            if (!string.IsNullOrEmpty(Timezone))
            {
                try
                {
                    //This is a rudimentary first version. Some potential problems that might arise,
                    //causing this to provide inaccurate results:
                    // - At the bottom, we attempt to account for DST changes, but by converting timezones,
                    //   I wouldn't be surprised if there were multiple DST or similar changes in between
                    // - Etc. (Timezones can be VERY weird. This could fail in a variety of ways)

                    TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(Timezone); //I could use https://github.com/mattjohnsonpint/TimeZoneConverter to support IANA timezones

                    next = TimeZoneInfo.ConvertTime(next.Value, timeZoneInfo);
                }
                catch (TimeZoneNotFoundException)
                {
                    throw new Exception($"Unrecognized timezone '{Timezone}'");
                }
            }

            //Using UTC converts here gives us the correct TimeSpan even if there is a DST change in between
            return TimeZoneInfo.ConvertTimeToUtc(next.Value) - TimeZoneInfo.ConvertTimeToUtc(fromDateTime);
        }
    }
}