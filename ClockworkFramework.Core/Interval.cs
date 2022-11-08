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

        public Interval(TimeType timeType, int frequency, int hour, int minute)
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

        public Interval(DayOfWeek dayOfWeek, int frequency, int hour, int minute)
        {
            TimeType = TimeType.Week;
            DayOfWeek = dayOfWeek;
            Frequency = frequency;
            Hour = hour;
            Minute = minute;
        }

        public TimeType TimeType { get; }
        public int Frequency { get; }
        public DayOfWeek DayOfWeek { get; }
        public int Hour { get; }
        public int Minute { get; }

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
                    next = fromDateTime.AddDays(Frequency);
                    next = new DateTime(next.Value.Year, next.Value.Month, next.Value.Day, Hour, Minute, 0);
                    break;
                case TimeType.Week:
                    next = fromDateTime.AddDays(((int)DayOfWeek - (int)fromDateTime.DayOfWeek + 7) % 7);
                    
                    if (Frequency > 1)
                    {
                        next = next.Value.AddDays(7 * Frequency);
                    }

                    next = new DateTime(next.Value.Year, next.Value.Month, next.Value.Day, Hour, Minute, 0);
                    break;
                case TimeType.Month:
                    //Todo: not currently supported
                    break;
                case TimeType.Year:
                    next = fromDateTime.AddYears(Frequency);
                    next = new DateTime(next.Value.Year, next.Value.Month, next.Value.Day, Hour, Minute, 0);
                    break;
            }

            if (!next.HasValue)
            {
                throw new Exception("Unable to calculate next occurence");
            }

            //Using UTC converts here gives us the correct TimeSpan even if there is a DST change in between
            return TimeZoneInfo.ConvertTimeToUtc(next.Value) - TimeZoneInfo.ConvertTimeToUtc(fromDateTime);
        }
    }
}