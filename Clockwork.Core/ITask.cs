namespace Clockwork.Core
{
    public interface ITask
    {
        Interval Interval { get; }

        public void Setup() { }
        public void Run();
        public void Teardown() { }

        public void Catch(Exception e)
        {
            Console.WriteLine($"Exception thrown in task: {e.Message}\n\n{e.StackTrace}");
        }
    }
}
