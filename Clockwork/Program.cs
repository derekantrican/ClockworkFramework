using System.Reflection;
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

            IEnumerable<Type> tasks = LoadLibraryTasks(Path.GetFullPath(@"..\..\ClockworkTasks")); //Todo: load library paths from file
            if (!tasks.Any())
            {
                Utilities.WriteToConsoleWithColor($"No external tasks loaded. Loading internal example tasks instead.", ConsoleColor.Yellow);
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                IEnumerable<Type> exampleTasks = GetTasksFromAssembly(currentAssembly);
                tasks = exampleTasks;
            }

            foreach (Type type in tasks)
            {
                ITask task = (ITask)Activator.CreateInstance(type);
                Console.WriteLine($"Found and registered task {type.FullName}");

                runningTasks.Add(RunTaskPeriodicAsync(task));
            }

            Task.WaitAll(runningTasks.ToArray());
        }

        private static IEnumerable<Type> LoadLibraryTasks(string libraryLocation)
        {
            if (libraryLocation.EndsWith(".dll"))
            {
                return LoadTasksFromDll(libraryLocation);
            }
            else if (libraryLocation.EndsWith(".csproj"))
            {
                return BuildCsprojAndLoadTasksFromBin(libraryLocation);
            }
            else //Assume library is a folder
            {
                string libraryName = new DirectoryInfo(libraryLocation).Name;
                string binLocation = Path.Combine(libraryLocation, "bin");
                Console.WriteLine($"Loading library {libraryName}");
                
                string[] csprojs = Directory.GetFiles(libraryLocation, "*.csproj");
                if (csprojs.Length < 1)
                {
                    Utilities.WriteToConsoleWithColor($"No .csproj found at location {libraryLocation} . Please make sure you are specifying the immediate parent folder to a .csproj", ConsoleColor.Red);
                    return Enumerable.Empty<Type>();
                }

                return BuildCsprojAndLoadTasksFromBin(csprojs[0]);
            }
        }

        private static IEnumerable<Type> BuildCsprojAndLoadTasksFromBin(string csprojPath)
        {
            string libraryLocation = new FileInfo(csprojPath).DirectoryName;
            string binLocation = Path.Combine(libraryLocation, "bin");
            string libraryName = Path.GetFileNameWithoutExtension(csprojPath);

            if (!Directory.Exists(binLocation))
            {
                var result = Utilities.RunProcess("dotnet build", libraryLocation);
                if (result.ExitCode != 0)
                {
                    Utilities.WriteToConsoleWithColor($"Build of library was unsuccessful. Log output below.\n\n{result.StdOut}\n\n{result.StdErr}", ConsoleColor.Red);
                    return Enumerable.Empty<Type>();
                }
                else if (!Directory.Exists(binLocation))
                {
                    Utilities.WriteToConsoleWithColor($"Build of library was successful, but bin folder was not found. Make sure the csproj does not have a different OutputPath specified", ConsoleColor.Red);
                    return Enumerable.Empty<Type>();
                }
            }

            string[] dlls = Directory.GetFiles(binLocation, $"{libraryName}.dll", SearchOption.AllDirectories);
            if (dlls.Length < 1)
            {
                Utilities.WriteToConsoleWithColor($"No {libraryName}.dll found. Make sure the csproj does not have a different OutputPath specified", ConsoleColor.Red);
                return Enumerable.Empty<Type>();
            }

            return LoadTasksFromDll(dlls[0]);
        }

        private static IEnumerable<Type> LoadTasksFromDll(string dllPath)
        {
            string libraryName = Path.GetFileNameWithoutExtension(dllPath);
            Assembly asm = Assembly.LoadFile(dllPath);
            IEnumerable<Type> tasksInDll = asm.GetTypes().Where(t => typeof(ITask).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (!tasksInDll.Any())
            {
                Utilities.WriteToConsoleWithColor($"Library {libraryName} does not contain any tasks", ConsoleColor.Red);
            }

            return tasksInDll;
        }

        private static IEnumerable<Type> GetTasksFromAssembly(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => typeof(ITask).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
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
