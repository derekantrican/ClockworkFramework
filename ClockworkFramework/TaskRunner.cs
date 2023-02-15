using System.Reflection;
using ClockworkFramework.Core;

namespace ClockworkFramework
{
    public class TaskRunner
    {
        public static async Task RunTaskPeriodicAsync(IClockworkTaskBase taskBase, MethodInfo taskMethod, CancellationToken cancellationToken,
            Action<Exception> exceptionHandler = null, Action<Exception> taskExceptionHandler = null)
        {
            Interval interval = (taskMethod.GetCustomAttribute(typeof(IntervalAttribute)) as IntervalAttribute).Interval;

            while (true)
            {
                await Task.Delay(interval.CalculateTimeToNext(DateTime.Now), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await Task.Run(() =>
                {
                    Utilities.RunWithCatch(() =>
                    {
                        RunTaskMethod(taskBase, taskMethod, () => taskBase.Setup(), "setup", taskExceptionHandler);
                        RunTaskMethod(taskBase, taskMethod, () => taskMethod.Invoke(taskBase, null), additionalTaskExceptionHandler: taskExceptionHandler);
                        RunTaskMethod(taskBase, taskMethod, () => taskBase.Teardown(), "teardown", taskExceptionHandler);
                    },
                    ex => 
                    {
                        Console.WriteLine($"[{DateTime.Now}] Task '{taskMethod.Name}' catch failed: ${ex.Message}\n{ex.StackTrace}");
                        exceptionHandler?.Invoke(ex);
                    });
                });
            }
        }

        private static void RunTaskMethod(IClockworkTaskBase taskBase, MethodInfo taskMethod, Action action, string methodName = "", Action<Exception> additionalTaskExceptionHandler = null)
        {
            Console.WriteLine($"[{DateTime.Now}] Running task '{taskMethod.Name}' {methodName}");

            Utilities.RunWithCatch(() => 
            {
                action();
                Console.WriteLine($"[{DateTime.Now}] Task '{taskMethod.Name}' {methodName} completed successfully");
            }, ex =>
            {
                Console.WriteLine($"[{DateTime.Now}] Task '{taskMethod.Name}' {methodName} failed");
                taskBase.Catch(ex);
                additionalTaskExceptionHandler?.Invoke(ex);
            });
        }
    }
}