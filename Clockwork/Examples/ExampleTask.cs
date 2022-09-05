namespace Clockwork.Core
{
    public class ExampleTask : ITask
    {
        public Interval Interval => new Interval(TimeType.Second, 30);

        //Optional: you can add a constructor to do any one-time setup when the instance is created

        public void Run()
        {
            Utilities.WriteToConsoleWithColor("ExampleTask is running", ConsoleColor.Yellow);
        }
    }
}
