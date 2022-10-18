using Newtonsoft.Json;
using ClockworkFramework.Core;

namespace Clockwork
{
    /*
    Ideas:
    - Should take adavantage of Windows Task Scheduler for an install script & to restart itself every night (in case it stops running).
      Can reference https://github.com/derekantrican/MountainProject/blob/master/MountainProjectBot/ScheduleTasks.bat
    */
    class Program
    {
        private static Config config = new Config();
        private static List<IClockworkTask> tasks = new List<IClockworkTask>();

        private static void Main(string[] args)
        {
            //Todo: support a "verbosity" arg that won't output setup, teardown, etc messages
            LoadConfig();
            RegisterAndRunTasks().Wait();
        }

        private static void LoadConfig()
        {
            string configFile = "config.json";
            if (!File.Exists(configFile))
            {
                Utilities.WriteToConsoleWithColor("No config file found. Using default settings.", ConsoleColor.Yellow);
                return;
            }

            try
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFile));
            }
            catch (Exception ex)
            {
                Utilities.WriteToConsoleWithColor($"Failed to load config: {ex.Message}\n{ex.StackTrace}", ConsoleColor.Red);
            }
        }

        private class TaskConfiguration
        {
            public Config.Library Source { get; set; }
            public Type TaskType { get; set; }
            public IClockworkTask Instance { get; set; }
            public Task RunningTask { get; set; }
            public CancellationTokenSource CancellationToken { get; set; } = new CancellationTokenSource();
        }

        private static async Task RegisterAndRunTasks()
        {
            List<Task> runningTasks = new List<Task>();

            List<TaskConfiguration> tasks = new List<TaskConfiguration>();
            foreach (Config.Library library in config.Libraries)
            {
                tasks.AddRange(TaskLoader.LoadLibraryTasks(Path.GetFullPath(library.Path)).Select(t => new TaskConfiguration
                {
                    Source = library,
                    TaskType = t,
                }));
            }

            if (tasks.Count == 0)
            {
                Utilities.WriteToConsoleWithColor($"No external tasks loaded. Loading internal example tasks instead.", ConsoleColor.Yellow);
                tasks.AddRange(TaskLoader.LoadExampleTasks().Select(t => new TaskConfiguration { TaskType = t }));
            }

            foreach (TaskConfiguration task in tasks)
            {
                task.Instance = (IClockworkTask)Activator.CreateInstance(task.TaskType);
                Console.WriteLine($"Found and registered task {task.TaskType.FullName}");
                
                task.RunningTask = TaskRunner.RunTaskPeriodicAsync(task.Instance, task.CancellationToken.Token);
                runningTasks.Add(task.RunningTask);
            }

            if (config.RepositoryUpdateFrequency > 0)
            {
                //Todo: should Clockwork have a way of (optionally) updating itself?
                runningTasks.Add(Task.Run(async () => 
                {
                    while (true)
                    {
                        await Task.Run(() =>
                        {
                            foreach (Config.Library library in config.Libraries)
                            {
                                Console.WriteLine($"Updating library {library.Path/*Todo: should have a better way of accessing the name*/}");

                                var result = Utilities.RunProcess("git pull", library.Path);
                                if (result.ExitCode != 0)
                                {
                                    Utilities.WriteToConsoleWithColor($"Failed to update library {library.Path}. Log output below.\n\n{result.StdOut}\n\n{result.StdErr}", ConsoleColor.Red);
                                    continue;
                                }

                                Utilities.WriteToConsoleWithColor(result.StdOut, ConsoleColor.DarkGreen);

                                if (result.StdOut.Contains("Already up to date"))
                                {
                                    Utilities.WriteToConsoleWithColor("Library is already up to date", ConsoleColor.DarkGreen);
                                    continue;
                                }

                                Utilities.WriteToConsoleWithColor("Cancelling current library tasks", ConsoleColor.DarkGreen);

                                foreach (TaskConfiguration task in tasks.Where(t => t.Source == library).ToList())
                                {
                                    Utilities.WriteToConsoleWithColor($"Canceling task {task.Instance}", ConsoleColor.DarkGreen);
                                    task.CancellationToken.Cancel();
                                    tasks.Remove(task);
                                }

                                Utilities.WriteToConsoleWithColor("Loading new version of library tasks", ConsoleColor.DarkGreen);

                                foreach (Type taskType in TaskLoader.LoadLibraryTasks(Path.GetFullPath(library.Path), true))
                                {
                                    TaskConfiguration task = new TaskConfiguration
                                    {
                                        Source = library,
                                        TaskType = taskType,
                                        Instance = (IClockworkTask)Activator.CreateInstance(taskType),
                                    };

                                    task.RunningTask = TaskRunner.RunTaskPeriodicAsync(task.Instance, task.CancellationToken.Token);
                                    tasks.Add(task);
                                    runningTasks.Add(task.RunningTask);
                                }

                                //Todo: add the option for some sort of hook (eg Discord message) to indicate that the library is up to date (commit id can
                                // be included based on the result.StdOut). Maybe the library can also have a version of Config.cs or something so hooks can be customized?
                            }
                        });

                        await Task.Delay(TimeSpan.FromMinutes(config.RepositoryUpdateFrequency));
                    }
                }));
            }

            //https://stackoverflow.com/a/50440399/2246411 wait while also allowing runningTasks to be changed
            while (runningTasks.Any(t => !t.IsCompleted)) 
            {
                await Task.WhenAll(runningTasks);
            }
        }
    }
}
