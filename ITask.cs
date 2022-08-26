using System;

namespace Clockwork
{
    /*
    Ideas:
    - Could have other methods like Setup(), Catch(Exception e), Teardown()
    - Could have some helper methods that JSON serlalize/deserialize to a file like Load(obj, file) and Save(obj, file)
    */
    public interface ITask
    {
        Interval Interval { get; }
        public void Run();

        public void Catch(Exception e)
        {
            Console.WriteLine($"Exception thrown in task: {e.Message}\n\n{e.StackTrace}");
        }
    }
}
