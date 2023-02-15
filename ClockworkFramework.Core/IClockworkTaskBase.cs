
namespace ClockworkFramework.Core
{
    public interface IClockworkTaskBase
    {
        public void Setup() { }
        public void Teardown() { }

        public void Catch(Exception e)
        {
            Console.WriteLine("Exception thrown in task:");
            Utilities.ProcessFullException(e, ex => Console.WriteLine($"{e.Message}\n\n{e.StackTrace}"));
        }
    }
}