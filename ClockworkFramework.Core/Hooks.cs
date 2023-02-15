
namespace ClockworkFramework.Core
{
    public abstract class Hooks
    {
        public abstract void LibraryUpdated(string libraryName, string message);
        public abstract void Warning(string message);
        public abstract void SystemExceptionHook(Exception exception, bool? isTerminating = null);
        
        //WARNING: this hook could be fired many times (eg if a task with an interval of "every minute"
        //starts failing, this hook will be called every minute). If you override this hook, it is recommended
        //that you also implement some sort of "cutoff", caching, etc to limit the effects.
        public virtual void GlobalTaskExceptionHook(Type taskType, System.Reflection.MethodInfo taskMethod, Exception exception) { }
    }
}