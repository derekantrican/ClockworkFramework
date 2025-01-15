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
                DateTime now = DateTime.Now;
                TimeSpan waitTime = interval.CalculateTimeToNext(now);
                DateTime nextExecution = now + waitTime;

                await Task.Delay(waitTime, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                //Due to some imprecision in Task.Delay (https://stackoverflow.com/a/31742754/2246411 - and other possible discrepancies
                //in the system clock like CMOS battery or whatever), CalculateTimeToNext could result in a time just before the next run
                //(eg 7:59:59.163 instead of 8:00:00.000). One problem this causes is double-triggering (eg task runs at 7:59:59.163 in less
                //than 500 ms and therefore calculates it should run again in ~400 ms at 8:00:00.000). This solves the "double-triggering"
                //issue by recalculating timeToNext if it is too soon
                //Notes:
                // - note that currently it is possible to create an Interval of "every X seconds" and a low number for X will probably
                //   cause problems with the below check (depending on how long the task takes to execute)
                // - I originally tried this with "> 0", but that also caused some problems in triggering (I don't remember if it was still
                //   "double-triggering" or "missing executions" or what)

                if ((nextExecution - DateTime.Now).TotalMilliseconds > 100) //If TotalMilliseconds is > 100, then "nextExecution" hasn't happened yet
                {
                    continue;
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