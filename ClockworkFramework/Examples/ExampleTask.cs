using ClockworkFramework.Core;

namespace ClockworkFramework.Examples
{
    public class ExampleTask : IClockworkTaskBase
    {
        //Optional: you can add a constructor to do any one-time setup when the instance is created
        //or you can add Setup() or Teardown() methods that will be called on each run. Additionally,
        //you can implement the Catch(Exception e) method to handle exceptions.

        [TaskMethod]
        [Interval(TimeType.Second, 30)]
        public void Run()
        {
            Utilities.WriteToConsoleWithColor("ExampleTask is running", ConsoleColor.Yellow);
        }
    }
}
