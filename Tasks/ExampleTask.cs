using System;

namespace Clockwork
{
    public class ExampleTask : ITask
    {
        public Interval Interval => new Interval(TimeType.Second, 30);

        //Optional: you can add a constructor to do any one-time setup when the instance is created

        public void Run()
        {
            Console.WriteLine("ExampleTask is running");
        }
    }
}
