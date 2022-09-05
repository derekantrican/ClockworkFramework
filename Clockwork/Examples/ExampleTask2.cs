namespace Clockwork.Core
{
    public class ExampleTask2 : ITask
    {
        public Interval Interval => new Interval(TimeType.Second, 10);

        //Optional: you can add a constructor to do any one-time setup when the instance is created

        public void Run()
        {
            Utilities.WriteToConsoleWithColor("ExampleTask2 is running", ConsoleColor.Yellow);
        }
    }
}
