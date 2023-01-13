
namespace ClockworkFramework.Core
{
    public abstract class Hooks
    {
        public abstract void GlobalCatch(Exception exception, bool? isTerminating = null);
        public abstract void Warning(string message);
        public abstract void LibraryUpdated(string libraryName, string message);
    }
}