
namespace ClockworkFramework.Core
{
    public class IntervalAttribute : Attribute
    {
        public Interval Interval { get; set; }

        public IntervalAttribute(TimeType timeType, int frequency)
        {
            Interval = new Interval(timeType, frequency);
        }

        public IntervalAttribute(TimeType timeType, int frequency, int hour, int minute)
        {
            Interval = new Interval(timeType, frequency, hour, minute);
        }

        public IntervalAttribute(DayOfWeek dayOfWeek, int frequency, int hour, int minute)
        {
            Interval = new Interval(dayOfWeek, frequency, hour, minute);
        }
    }
}