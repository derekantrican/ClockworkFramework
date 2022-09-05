﻿using System.Reflection;
using Clockwork.Core;

namespace Clockwork
{
    /*
    Ideas:
    - Should take adavantage of Windows Task Scheduler for an install script & to restart itself every night (in case it stops running).
      Can reference https://github.com/derekantrican/MountainProject/blob/master/MountainProjectBot/ScheduleTasks.bat
    - Should update tasks from github on a certain cadence (either once/day, once/hour, or even once/5min). Don't know if the whole program
      (the Clockwork framework) should shut down & update or just the tasks (maybe could do a `git pull` on just that folder https://stackoverflow.com/a/4048993/2246411).
      Tasks could even be a separate assembly that could be unloaded, updated, then re-registered. That way, 1) the git operations could be
      entirely within the C# code here (rather than batch) and 2) the "Tasks" would have a clear separation from the Clockwork framework
    - On the note of "a separate place for user-defined Tasks", the user could keep a separate repo for tasks, then in the Clockworks repo could specify something
      like a ".env" file with the Tasks library path (https://codingvision.net/c-load-dll-at-runtime) & an update (git pull) cadence. We would just need to 
      figure out how to safely load the library from a filepath.
    */
    class Program
    {
        private static List<ITask> tasks = new List<ITask>();

        private static void Main(string[] args)
        {
            Console.WriteLine(args.Length > 0 ? args[0] : "");
            
            RegisterAndRunTasks();
        }

        private static void RegisterAndRunTasks()
        {
            List<Task> runningTasks = new List<Task>();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(ITask).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
            {
                ITask task = (ITask)Activator.CreateInstance(type);
                Console.WriteLine($"Found and registered task {type.FullName}");

                runningTasks.Add(RunTaskPeriodicAsync(task));
            }

            Task.WaitAll(runningTasks.ToArray());
        }

        private static async Task RunTaskPeriodicAsync(ITask task)
        {
            while (true)
            {
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
                    });
                });

                await Task.Delay(task.Interval.CalculateTimeToNext(DateTime.Now));
            }
        }

        private static void RunTaskMethod(ITask task, Action action, string methodName = "")
        {
            Console.WriteLine($"[{DateTime.Now}] Running task '{task}' {methodName}");
            RunWithCatch(action, ex =>
            {
                Console.WriteLine($"[{DateTime.Now}] Task '{task}' {methodName} failed");
                task.Catch(ex);
            });
            Console.WriteLine($"[{DateTime.Now}] Task '{task}' {methodName} completed successfully");
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