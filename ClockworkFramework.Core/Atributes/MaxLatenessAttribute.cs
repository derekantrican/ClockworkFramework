
namespace ClockworkFramework.Core
{
    /// <summary>
    /// Specifies the maximum amount of time a task can be late and still run.
    /// If the task is later than this threshold (e.g. due to system sleep/hibernate),
    /// it will be skipped and rescheduled for the next occurrence.
    /// 
    /// If this attribute is omitted, a default grace period of 60 minutes is used.
    /// Use MaxLateness(-1) to indicate "always run, no matter how late".
    /// </summary>
    public class MaxLatenessAttribute : Attribute
    {
        public TimeSpan MaxLateness { get; set; }

        /// <summary>
        /// Specify max lateness in minutes. Use -1 for "always run, no matter how late".
        /// </summary>
        public MaxLatenessAttribute(int minutes)
        {
            MaxLateness = minutes < 0 ? TimeSpan.MaxValue : TimeSpan.FromMinutes(minutes);
        }

        /// <summary>
        /// Specify max lateness with a TimeType and value. E.g. (TimeType.Hour, 2) for 2 hours.
        /// Only Second, Minute, Hour, and Day are supported here.
        /// </summary>
        public MaxLatenessAttribute(TimeType timeType, int value)
        {
            if (value < 0)
            {
                MaxLateness = TimeSpan.MaxValue;
                return;
            }

            MaxLateness = timeType switch
            {
                TimeType.Second => TimeSpan.FromSeconds(value),
                TimeType.Minute => TimeSpan.FromMinutes(value),
                TimeType.Hour => TimeSpan.FromHours(value),
                TimeType.Day => TimeSpan.FromDays(value),
                _ => throw new ArgumentException($"TimeType.{timeType} is not supported for MaxLateness. Use Second, Minute, Hour, or Day."),
            };
        }
    }
}
