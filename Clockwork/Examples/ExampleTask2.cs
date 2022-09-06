namespace Clockwork.Core
{
    public class ExampleTask2 : ITask
    {
        public Interval Interval => new Interval(TimeType.Second, 10);

        //Optional: you can add a constructor to do any one-time setup when the instance is created
        //or you can add Setup() or Teardown() methods that will be called on each run. Additionally,
        //you can implement the Catch(Exception e) method to handle exceptions.

        public void Run()
        {
            Utilities.WriteToConsoleWithColor("ExampleTask2 is running", ConsoleColor.Yellow);
        }
    }
}
