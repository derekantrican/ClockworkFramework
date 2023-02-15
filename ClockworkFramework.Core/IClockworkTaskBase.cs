
namespace ClockworkFramework.Core
{
    public interface IClockworkTaskBase
    {
        public void Setup() { }
        public void Teardown() { }

        public void Catch(Exception e)
        {
            Console.WriteLine("Exception thrown in task:");
            Utilities.ProcessFullException(e, ex => Console.WriteLine($"{ex.Message}\n\n{ex.StackTrace}"));
        }
    }
}