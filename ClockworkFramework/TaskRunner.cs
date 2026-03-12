using System.Reflection;
using ClockworkFramework.Core;

namespace ClockworkFramework
{
    public class TaskRunner
    {
        public static async Task RunTaskPeriodicAsync(IClockworkTaskBase taskBase, MethodInfo taskMethod, CancellationToken cancellationToken,
            Action<Exception> exceptionHandler = null, Action<Exception> taskExceptionHandler = null, Action<string> warningHandler = null)
        {
            Interval interval = (taskMethod.GetCustomAttribute(typeof(IntervalAttribute)) as IntervalAttribute).Interval;

            // Read the MaxLateness attribute if present; default to 60 minutes if omitted
            var maxLatenessAttr = taskMethod.GetCustomAttribute(typeof(MaxLatenessAttribute)) as MaxLatenessAttribute;
            TimeSpan maxLateness = maxLatenessAttr?.MaxLateness ?? TimeSpan.FromMinutes(60);

            while (true)
            {
                TimeSpan waitTime;
                DateTime nextExecution;
                try
                {
                    DateTime now = DateTime.Now;
                    waitTime = interval.CalculateTimeToNext(now);
                    nextExecution = now + waitTime;
                }
                catch (Exception ex)
                {
                    // If CalculateTimeToNext fails (e.g. due to an unexpected DST edge case),
                    // log the error and retry after a short delay rather than permanently killing the task
                    Console.WriteLine($"[{DateTime.Now}] Task '{taskMethod.Name}' failed to calculate next run time: {ex.Message}. Retrying in 60 seconds.");
                    exceptionHandler?.Invoke(ex);
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                    continue;
                }

                // Instead of a single Task.Delay for the full wait (which doesn't tick during
                // system sleep/hibernate, causing tasks to fire late after wake), we cap each
                // delay at MaxDelayInterval and re-check the wall clock. This makes long waits
                // resilient to sleep/hibernate/power outage scenarios.
                TimeSpan MaxDelayInterval = TimeSpan.FromMinutes(1);
                while (waitTime > MaxDelayInterval)
                {
                    await Task.Delay(MaxDelayInterval, cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Recalculate from current wall-clock time to stay accurate after sleep/wake
                    waitTime = nextExecution - DateTime.Now;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (waitTime > TimeSpan.Zero)
                {
                    await Task.Delay(waitTime, cancellationToken);
                }

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

                // Check if the task is late and warn/skip accordingly
                TimeSpan lateness = DateTime.Now - nextExecution;
                if (lateness > maxLateness)
                {
                    string skipMessage = $"Task '{taskMethod.Name}' skipped: {lateness.TotalMinutes:F1} min late exceeds max lateness of {(maxLateness == TimeSpan.MaxValue ? "unlimited" : $"{maxLateness.TotalMinutes:F0} min")}";
                    Console.WriteLine($"[{DateTime.Now}] {skipMessage}");
                    warningHandler?.Invoke(skipMessage);
                    continue;
                }
                else if (lateness.TotalSeconds > 30) // Only warn if meaningfully late (not just minor scheduling jitter)
                {
                    string lateMessage = $"Task '{taskMethod.Name}' has started ({lateness.TotalMinutes:F1} min late)";
                    Console.WriteLine($"[{DateTime.Now}] {lateMessage}");
                    warningHandler?.Invoke(lateMessage);
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
                        Console.WriteLine($"[{DateTime.Now}] Task '{taskMethod.Name}' catch failed: {ex.Message}\n{ex.StackTrace}");
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