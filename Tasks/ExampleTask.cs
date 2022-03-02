using System;

namespace Clockwork
{
    public class ExampleTask : ITask
    {
        public TimeSpan Interval => TimeSpan.FromSeconds(30);

        //Optional: you can add a constructor to do any one-time setup when the instance is created

        public void Run()
        {
            Console.WriteLine("ExampleTask is running");
        }
    }
}
