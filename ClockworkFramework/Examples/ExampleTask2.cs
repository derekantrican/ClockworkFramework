using ClockworkFramework.Core;

namespace ClockworkFramework.Examples
{
    public class ExampleTask2 : IClockworkTaskBase
    {
        //Optional: you can add a constructor to do any one-time setup when the instance is created
        //or you can add Setup() or Teardown() methods that will be called on each run. Additionally,
        //you can implement the Catch(Exception e) method to handle exceptions.

        [TaskMethod]
        [Interval(TimeType.Second, 10)]
        public void Run()
        {
            Utilities.WriteToConsoleWithColor("ExampleTask2 is running", ConsoleColor.Yellow);
        }
    }
}
