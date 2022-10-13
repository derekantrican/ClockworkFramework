using Clockwork.Core;

namespace Clockwork.Examples
{
    public class ExampleTask : ITask
    {
        public Interval Interval => new Interval(TimeType.Second, 30);

        //Optional: you can add a constructor to do any one-time setup when the instance is created
        //or you can add Setup() or Teardown() methods that will be called on each run. Additionally,
        //you can implement the Catch(Exception e) method to handle exceptions.

        public void Run()
        {
            Utilities.WriteToConsoleWithColor("ExampleTask is running", ConsoleColor.Yellow);
        }
    }
}
