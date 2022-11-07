using System.Reflection;
using ClockworkFramework.Core;

namespace ClockworkFramework
{
    public class TaskRunner
    {
        public static async Task RunTaskPeriodicAsync(IClockworkTaskBase taskBase, MethodInfo taskMethod, CancellationToken cancellationToken, Action<Exception> exceptionHandler = null)
        {
            Interval interval = (taskMethod.GetCustomAttribute(typeof(IntervalAttribute)) as IntervalAttribute).Interval;

            while (true)
            {
                await Task.Delay(interval.CalculateTimeToNext(DateTime.UtcNow), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await Task.Run(() =>
                {
                    RunWithCatch(() =>
                    {
                        RunTaskMethod(taskBase, taskMethod, () => taskBase.Setup(), "setup");
                        RunTaskMethod(taskBase, taskMethod, () => taskMethod.Invoke(taskBase, null));
                        RunTaskMethod(taskBase, taskMethod, () => taskBase.Teardown(), "teardown");
                    },
                    ex => 
                    {
                        Console.WriteLine($"[{DateTime.Now}] Task '{taskMethod.Name}' catch failed: ${ex.Message}\n{ex.StackTrace}");
                        exceptionHandler?.Invoke(ex);
                    });
                });
            }
        }

        private static void RunTaskMethod(IClockworkTaskBase taskBase, MethodInfo taskMethod, Action action, string methodName = "")
        {
            Console.WriteLine($"[{DateTime.Now}] Running task '{taskMethod.Name}' {methodName}");

            RunWithCatch(() => 
            {
                action();
                Console.WriteLine($"[{DateTime.Now}] Task '{taskMethod.Name}' {methodName} completed successfully");
            }, ex =>
            {
                Console.WriteLine($"[{DateTime.Now}] Task '{taskMethod.Name}' {methodName} failed");
                taskBase.Catch(ex);
            });
        }

        private static void RunWithCatch(Action action, Action<Exception> onException)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                onException(e);
            }
        }
    }
}