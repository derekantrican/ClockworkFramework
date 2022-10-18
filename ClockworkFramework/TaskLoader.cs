using System.Reflection;
using ClockworkFramework.Core;

namespace Clockwork
{
    public static class TaskLoader
    {
        public static IEnumerable<Type> LoadLibraryTasks(string libraryLocation, bool forceRebuildIfApplicable = false)
        {
            if (libraryLocation.EndsWith(".dll"))
            {
                return LoadTasksFromDll(libraryLocation);
            }
            else if (libraryLocation.EndsWith(".csproj"))
            {
                return BuildCsprojAndLoadTasksFromBin(libraryLocation, forceRebuildIfApplicable);
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

                return BuildCsprojAndLoadTasksFromBin(csprojs[0], forceRebuildIfApplicable);
            }
        }

        private static IEnumerable<Type> BuildCsprojAndLoadTasksFromBin(string csprojPath, bool forceRebuildIfApplicable = false)
        {
            string libraryLocation = new FileInfo(csprojPath).DirectoryName;
            string binLocation = Path.Combine(libraryLocation, "bin");
            string libraryName = Path.GetFileNameWithoutExtension(csprojPath);

            if (!Directory.Exists(binLocation) || forceRebuildIfApplicable)
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
            Assembly asm = Assembly.Load(File.ReadAllBytes(dllPath));
            IEnumerable<Type> tasksInDll = asm.GetTypes().Where(t => typeof(IClockworkTask).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (!tasksInDll.Any())
            {
                Utilities.WriteToConsoleWithColor($"Library {libraryName} does not contain any tasks", ConsoleColor.Red);
            }

            return tasksInDll;
        }

        public static IEnumerable<Type> LoadExampleTasks()
        {
            return GetTasksFromAssembly(Assembly.GetExecutingAssembly()).Where(t => t.Namespace == "Clockwork.Examples");
        }

        private static IEnumerable<Type> GetTasksFromAssembly(Assembly assembly)
        {
            return assembly.GetTypes().Where(t => typeof(IClockworkTask).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        }
    }
}