namespace Clockwork.Core
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
                throw new ArgumentException("For TimeType.Day or TimeType.Year, use the constructor Interval(timeType, frequency, hour, minute)");
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
            DateTime next;
            switch (TimeType)
            {
                case TimeType.Second:
                    return fromDateTime.AddSeconds(Frequency) - fromDateTime;
                case TimeType.Minute:
                    return fromDateTime.AddMinutes(Frequency) - fromDateTime;
                case TimeType.Hour:
                    return fromDateTime.AddHours(Frequency) - fromDateTime;
                case TimeType.Day:
                    next = fromDateTime.AddDays(Frequency);
                    return new DateTime(next.Year, next.Month, next.Day, Hour, Minute, 0) - fromDateTime;
                case TimeType.Week:
                    next = fromDateTime.AddDays(((int)DayOfWeek - (int)fromDateTime.DayOfWeek + 7) % 7);
                    
                    if (Frequency > 1)
                    {
                        next = next.AddDays(7 * Frequency);
                    }

                    return new DateTime(next.Year, next.Month, next.Day, Hour, Minute, 0) - fromDateTime;
                case TimeType.Month:
                    //Todo: not currently supported
                    break;
                case TimeType.Year:
                    next = fromDateTime.AddYears(Frequency);
                    return new DateTime(next.Year, next.Month, next.Day, Hour, Minute, 0) - fromDateTime;
            }

            throw new Exception("Unable to calculate next occurence");
        }
    }
}