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
            
            RegisterAndRunTasks();
        }

        private static void RegisterAndRunTasks()
        {
            List<Task> runningTasks = new List<Task>();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t => typeof(ITask).IsAssignableFrom(t) && ! t.IsInterface && !t.IsAbstract))
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

                await Task.Delay(task.Interval.CalculateTimeToNext(DateTime.Now));
            }
        }
    }
}
