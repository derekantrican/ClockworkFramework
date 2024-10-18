using System.Text.RegularExpressions;
using System.Reflection;
using Newtonsoft.Json;
using ClockworkFramework.Core;

namespace ClockworkFramework
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
        private static List<IClockworkTaskBase> tasks = new List<IClockworkTaskBase>();

        private static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            //Todo: support a "verbosity" arg that won't output setup, teardown, etc messages
            LoadConfig();
            await RegisterAndRunTasks();
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e) 
        {
            Exception ex = (Exception)e.ExceptionObject;
            Utilities.WriteToConsoleWithColor($"Unhandled Exception encountered: {ex.Message}\n{ex.StackTrace}", ConsoleColor.Red);
            CallHook(h => h.SystemExceptionHook(ex, e.IsTerminating));
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
            public IClockworkTaskBase Instance { get; set; }
            public MethodInfo TaskMethod { get; set; }
            public Task RunningTask { get; set; }
            public CancellationTokenSource CancellationToken { get; set; } = new CancellationTokenSource();
        }

        private static async Task RegisterAndRunTasks()
        {
            List<Task> runningTasks = new List<Task>();

            List<TaskConfiguration> tasks = new List<TaskConfiguration>();
            foreach (Library library in config.Libraries)
            {
                tasks.AddRange(TaskLoader.LoadLibraryTasks(library).SelectMany(t => TaskLoader.LoadTaskMethodsFromClassType(t)).Select(m => new TaskConfiguration
                {
                    Source = library,
                    TaskType = m.DeclaringType,
                    TaskMethod = m,
                }));
                
                hooks[library] = TaskLoader.GetTypesOfTypeFromAssembly(library.Assembly, typeof(Hooks)).Select(h => (Hooks)Activator.CreateInstance(h)).ToList();
            }

            if (tasks.Count == 0)
            {
                Utilities.WriteToConsoleWithColor($"No external tasks loaded. Loading internal example tasks instead.", ConsoleColor.Yellow);
                tasks.AddRange(TaskLoader.LoadExampleTasks().Select(m => new TaskConfiguration
                { 
                    TaskType = m.DeclaringType,
                    TaskMethod = m,
                }));
            }

            foreach (TaskConfiguration task in tasks)
            {
                task.Instance = (IClockworkTaskBase)Activator.CreateInstance(task.TaskType);
                Console.WriteLine($"Found and registered task {task.TaskType.Name}.{task.TaskMethod.Name}");
                
                task.RunningTask = TaskRunner.RunTaskPeriodicAsync(task.Instance, task.TaskMethod, task.CancellationToken.Token,
                    ex => CallHook(h => h.SystemExceptionHook(ex)), ex => CallHook(h => h.GlobalTaskExceptionHook(task.TaskType, task.TaskMethod, ex)));
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
                                if (!library.UpdateRepository)
                                {
                                    continue;
                                }

                                Console.WriteLine($"Updating library {library.Name}");
                                
                                var result = Utilities.RunProcess("git", "pull", library.Path, TimeSpan.FromMinutes(15));
                                if (result.TimedOut || result.ExitCode != 0)
                                {
                                    Utilities.WriteToConsoleWithColor($"Failed to update library {library.Name} ({result.ExitCode}). Log output below.\n\n{result.StdOut}\n\n{result.StdErr}", ConsoleColor.Red);

                                    string message = $"'git pull' failed for library {library.Name}.";
                                    if (result.StdErr.Contains("not a git repository"))
                                    {
                                        message += " If this is not a git repository, turn off updateRepository in config.json";
                                    }

                                    if (result.TimedOut)
                                    {
                                        message += " Process timed out. Perhaps it was waiting for input? Try storing your git credentials";
                                    }

                                    CallHook(h => h.Warning(message));

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
                                    Utilities.WriteToConsoleWithColor($"Canceling task {task.TaskType.Name}.{task.TaskMethod.Name}", ConsoleColor.DarkGreen);
                                    task.CancellationToken.Cancel();
                                    tasks.Remove(task);
                                }

                                //Unload hooks for library
                                hooks.Remove(library);

                                //Reload hooks for library
                                hooks[library] = TaskLoader.GetTypesOfTypeFromAssembly(library.Assembly, typeof(Hooks)).Select(h => (Hooks)Activator.CreateInstance(h)).ToList();

                                Utilities.WriteToConsoleWithColor("Loading new version of library tasks", ConsoleColor.DarkGreen);

                                //Reload tasks for library
                                foreach (Type taskType in TaskLoader.LoadLibraryTasks(library, true))
                                {
                                    foreach (MethodInfo taskMethod in TaskLoader.LoadTaskMethodsFromClassType(taskType))
                                    {
                                        TaskConfiguration task = new TaskConfiguration
                                        {
                                            Source = library,
                                            TaskType = taskType,
                                            TaskMethod = taskMethod,
                                            Instance = (IClockworkTaskBase)Activator.CreateInstance(taskType),
                                        };

                                        Console.WriteLine($"Found and registered task {task.TaskType.Name}.{task.TaskMethod.Name}");

                                        task.RunningTask = TaskRunner.RunTaskPeriodicAsync(task.Instance, task.TaskMethod, task.CancellationToken.Token,
                                            ex => CallHook(h => h.SystemExceptionHook(ex)), ex => CallHook(h => h.GlobalTaskExceptionHook(task.TaskType, task.TaskMethod, ex)));
                                        tasks.Add(task);
                                        runningTasks.Add(task.RunningTask);
                                    }
                                }

                                string newCommit = Regex.Match(result.StdOut, @"(?<commit1>[a-z0-9]*)\.\.(?<commit2>[a-z0-9]*)").Groups["commit2"].Value;
                                string commitMsg = Utilities.RunProcess("git", $"show --pretty=format:\"%B\" --no-patch {newCommit}", library.Path).StdOut.Trim();

                                CallHook(h => h.LibraryUpdated(library.Name, $"Library {library.Name} has been updated to {newCommit} (\"{commitMsg}\")"));
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
                Utilities.RunWithCatch(() => hookAction?.Invoke(hook), ex => Console.WriteLine($"[{DateTime.Now}] Hook {hookAction} threw an exception: ${ex.Message}\n{ex.StackTrace}"));
            }
        }
    }
}
