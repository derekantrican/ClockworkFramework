
using ClockworkFramework.Core;

namespace ClockworkFramework.Examples
{
    public class ExampleMultiRunTask : IClockworkTaskBase
    {
        //You can define multiple [TaskMethod] per IClockworkTaskBase, each with its own [Interval].
        //Each of these TaskMethods uses the same Setup, Teardown, & Catch method definitions

        [TaskMethod]
        [Interval(TimeType.Minute, 1)] //Each run method should have its own [TaskMethod] attribute and [Interval] attribute
        public void RunEvery1Minute()
        {
            Utilities.WriteToConsoleWithColor("MultiRun: every 1 minute", ConsoleColor.Blue);
        }

        [TaskMethod]
        [Interval(TimeType.Minute, 2)]
        public void RunEvery2Minutes()
        {
            Utilities.WriteToConsoleWithColor("MultiRun: every 2 minutes", ConsoleColor.Blue);
        }

        [TaskMethod]
        [Interval(TimeType.Minute, 3)]
        public void RunEvery3Minutes()
        {
            Utilities.WriteToConsoleWithColor("MultiRun: every 3 minutes", ConsoleColor.Blue);
        }
    }
}