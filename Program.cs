using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Clockwork
{
    class Program
    {
        private static List<ITask> tasks = new List<ITask>();

        private static void Main(string[] args)
        {
            Console.WriteLine(args.Length > 0 ? args[0] : "");
            
            RegisterTasks();
            RunTasks();
        }

        private static void RegisterTasks()
        {
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(ITask).IsAssignableFrom(t) && ! t.IsInterface && !t.IsAbstract))
            {
                tasks.Add((ITask)Activator.CreateInstance(type));
                Console.WriteLine($"Found and registered task {type.FullName}");
            }
        }

        private static void RunTasks()
        {
            List<Task> runningTasks = new List<Task>();

            foreach (ITask task in tasks)
            {
                runningTasks.Add(RunTaskPeriodicAsync(task, CancellationToken.None));
            }

            Task.WaitAll(runningTasks.ToArray());
        }

        private static async Task RunTaskPeriodicAsync(ITask task, CancellationToken cancellationToken)
        {
            while (true)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Console.WriteLine($"[{DateTime.Now}] Running task '{task}'");
                        task.Run();
                        Console.WriteLine($"[{DateTime.Now}] Task '{task}' completed successfully");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Task '{task}' failed");
                        task.Catch(e);
                    }
                });

                await Task.Delay(task.Interval, cancellationToken);
            }
        }
    }
}
