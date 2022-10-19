﻿using System.Text.RegularExpressions;
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
        private static Dictionary<Library, List<Hooks>> hooks = new Dictionary<Library, List<Hooks>>();
        private static List<IClockworkTask> tasks = new List<IClockworkTask>();

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            //Todo: support a "verbosity" arg that won't output setup, teardown, etc messages
            LoadConfig();
            RegisterAndRunTasks().Wait();
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) 
        {
            Exception ex = (Exception)e.ExceptionObject;
            Utilities.WriteToConsoleWithColor($"Unhandled Exception encountered: {ex.Message}\n{ex.StackTrace}", ConsoleColor.Red);
            CallHook(h => h.GlobalCatch(ex));
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
            public Library Source { get; set; }
            public Type TaskType { get; set; }
            public IClockworkTask Instance { get; set; }
            public Task RunningTask { get; set; }
            public CancellationTokenSource CancellationToken { get; set; } = new CancellationTokenSource();
        }

        private static async Task RegisterAndRunTasks()
        {
            List<Task> runningTasks = new List<Task>();

            List<TaskConfiguration> tasks = new List<TaskConfiguration>();
            foreach (Library library in config.Libraries)
            {
                tasks.AddRange(TaskLoader.LoadLibraryTasks(library).Select(t => new TaskConfiguration
                {
                    Source = library,
                    TaskType = t,
                }));
                
                hooks[library] = TaskLoader.GetTypesOfTypeFromAssembly(library.Assembly, typeof(Hooks)).Select(h => (Hooks)Activator.CreateInstance(h)).ToList();
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
                
                task.RunningTask = TaskRunner.RunTaskPeriodicAsync(task.Instance, task.CancellationToken.Token, ex => CallHook(h => h.GlobalCatch(ex)));
                runningTasks.Add(task.RunningTask);
            }

            if (config.RepositoryUpdateFrequency > 0)
            {
                //Todo: should Clockwork have a way of (optionally) updating itself? (Maybe not - if the framework is one version,
                // but the library is using a different version that could cause issues)
                runningTasks.Add(Task.Run(async () => 
                {
                    while (true)
                    {
                        await Task.Run(() =>
                        {
                            foreach (Library library in config.Libraries)
                            {
                                Console.WriteLine($"Updating library {library.Name}");

                                var result = Utilities.RunProcess("git pull", library.Path);
                                if (result.ExitCode != 0)
                                {
                                    Utilities.WriteToConsoleWithColor($"Failed to update library {library.Name}. Log output below.\n\n{result.StdOut}\n\n{result.StdErr}", ConsoleColor.Red);
                                    CallHook(h => h.Warning($"'git pull' failed for library {library.Name}. If this is not a git repository, turn off updateRepository in config.json"));
                                    continue;
                                }

                                Utilities.WriteToConsoleWithColor(result.StdOut, ConsoleColor.DarkGreen);

                                if (result.StdOut.Contains("Already up to date"))
                                {
                                    Utilities.WriteToConsoleWithColor("Library is already up to date", ConsoleColor.DarkGreen);
                                    continue;
                                }

                                Utilities.WriteToConsoleWithColor("Cancelling current library tasks", ConsoleColor.DarkGreen);

                                //Unload tasks for library
                                foreach (TaskConfiguration task in tasks.Where(t => t.Source == library).ToList())
                                {
                                    Utilities.WriteToConsoleWithColor($"Canceling task {task.Instance}", ConsoleColor.DarkGreen);
                                    task.CancellationToken.Cancel();
                                    tasks.Remove(task);
                                }

                                //Unload hooks for library
                                hooks.Remove(library);

                                Utilities.WriteToConsoleWithColor("Loading new version of library tasks", ConsoleColor.DarkGreen);

                                //Reload tasks for library
                                foreach (Type taskType in TaskLoader.LoadLibraryTasks(library, true))
                                {
                                    TaskConfiguration task = new TaskConfiguration
                                    {
                                        Source = library,
                                        TaskType = taskType,
                                        Instance = (IClockworkTask)Activator.CreateInstance(taskType),
                                    };

                                    task.RunningTask = TaskRunner.RunTaskPeriodicAsync(task.Instance, task.CancellationToken.Token, ex => CallHook(h => h.GlobalCatch(ex)));
                                    tasks.Add(task);
                                    runningTasks.Add(task.RunningTask);
                                }

                                //Reload hooks for library
                                hooks[library] = TaskLoader.GetTypesOfTypeFromAssembly(library.Assembly, typeof(Hooks)).Select(h => (Hooks)Activator.CreateInstance(h)).ToList();

                                string newCommit = Regex.Match(result.StdOut, @"(?<commit1>[a-z0-9]*)\.\.(?<commit2>[a-z0-9]*)").Groups["commit2"].Value;
                                CallHook(h => h.LibraryUpdated(library.Name, $"Library {library.Name} has been updated to {newCommit}"));
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

        private static void CallHook(Action<Hooks> hookAction)
        {
            foreach (Hooks hook in hooks.SelectMany(h => h.Value))
            {
                hookAction?.Invoke(hook);
            }
        }
    }
}
