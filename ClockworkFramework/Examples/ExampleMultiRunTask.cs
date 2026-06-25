
using ClockworkFramework.Core;

namespace ClockworkFramework.Examples
{
    public class ExampleMultiRunTask : IClockworkTaskBase
    {
        //You can define multiple task methods per IClockworkTaskBase, each with its own [Interval].
        //Each of these task methods uses the same Setup, Teardown, & Catch method definitions

        [Interval(TimeType.Minute, 1)]
        public void RunEvery1Minute()
        {
            Utilities.WriteToConsoleWithColor("MultiRun: every 1 minute", ConsoleColor.Blue);
        }

        [Interval(TimeType.Minute, 2)]
        public void RunEvery2Minutes()
        {
            Utilities.WriteToConsoleWithColor("MultiRun: every 2 minutes", ConsoleColor.Blue);
        }

        [Interval(TimeType.Minute, 3)]
        public void RunEvery3Minutes()
        {
            Utilities.WriteToConsoleWithColor("MultiRun: every 3 minutes", ConsoleColor.Blue);
        }
    }
}