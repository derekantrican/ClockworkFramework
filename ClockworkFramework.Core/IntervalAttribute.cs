
namespace ClockworkFramework.Core
{
    public class IntervalAttribute : Attribute
    {
        public Interval Interval { get; set; }

        public IntervalAttribute(TimeType timeType, int frequency)
        {
            Interval = new Interval(timeType, frequency);
        }

        //Interval for "run every # (Hour|Day|Month|etc) [at X time]"
        public IntervalAttribute(TimeType timeType, int frequency, int hour, int minute)
        {
            Interval = new Interval(timeType, frequency, hour, minute);
        }

        //Interval for "run every # (Mon|Tues|Wed|etc) [at X time]"
        public IntervalAttribute(DayOfWeek dayOfWeek, int frequency, int hour, int minute)
        {
            Interval = new Interval(dayOfWeek, frequency, hour, minute);
        }

        //Interval for "run every # (Hour|Day|Month|etc) [at X time]" WITH timezone (https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/default-time-zones)
        public IntervalAttribute(TimeType timeType, int frequency, int hour, int minute, string timezone)
        {
            Interval = new Interval(timeType, frequency, hour, minute, timezone);
        }

        //Interval for "run every # (Mon|Tues|Wed|etc) [at X time]" WITH timezone (https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/default-time-zones)
        public IntervalAttribute(DayOfWeek dayOfWeek, int frequency, int hour, int minute, string timezone)
        {
            Interval = new Interval(dayOfWeek, frequency, hour, minute, timezone);
        }
    }
}