using System;

namespace Clockwork
{
    public class ExampleTask2 : ITask
    {
        public TimeSpan Interval => TimeSpan.FromSeconds(10);

        //Optional: you can add a constructor to do any one-time setup when the instance is created

        public void Run()
        {
            Console.WriteLine("ExampleTask2 is running");
        }
    }
}
