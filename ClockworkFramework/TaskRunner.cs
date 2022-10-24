using ClockworkFramework.Core;

namespace Clockwork
{
    public class TaskRunner
    {
        public static async Task RunTaskPeriodicAsync(IClockworkTask task, CancellationToken cancellationToken, Action<Exception> exceptionHandler = null)
        {
            while (true)
            {
                await Task.Delay(task.Interval.CalculateTimeToNext(DateTime.Now), cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await Task.Run(() =>
                {
                    RunWithCatch(() =>
                    {
                        RunTaskMethod(task, () => task.Setup(), "setup");
                        RunTaskMethod(task, () => task.Run());
                        RunTaskMethod(task, () => task.Teardown(), "teardown");
                    },
                    ex => 
                    {
                        Console.WriteLine($"[{DateTime.Now}] Task '{task}' catch failed: ${ex.Message}\n{ex.StackTrace}");
                        exceptionHandler?.Invoke(ex);
                    });
                });
            }
        }

        private static void RunTaskMethod(IClockworkTask task, Action action, string methodName = "")
        {
            Console.WriteLine($"[{DateTime.Now}] Running task '{task}' {methodName}");

            RunWithCatch(() => 
            {
                action();
                Console.WriteLine($"[{DateTime.Now}] Task '{task}' {methodName} completed successfully");
            }, ex =>
            {
                Console.WriteLine($"[{DateTime.Now}] Task '{task}' {methodName} failed");
                task.Catch(ex);
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