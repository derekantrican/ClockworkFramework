using System.Reflection;
using ClockworkFramework.Core;

namespace ClockworkFramework
{
    public static class TaskLoader
    {
        public static IEnumerable<Type> LoadLibraryTasks(Library library, bool forceRebuildIfApplicable = false)
        {
            string libraryLocation = Path.GetFullPath(library.Path);

            if (libraryLocation.EndsWith(".dll"))
            {
                library.Name = Path.GetFileNameWithoutExtension(libraryLocation);
                library.Assembly = LoadAssemblyFromDll(libraryLocation);
            }
            else if (libraryLocation.EndsWith(".csproj"))
            {
                library.Name = Path.GetFileNameWithoutExtension(libraryLocation);
                library.Assembly = BuildCsprojAndLoadAssemblyFromBin(libraryLocation, forceRebuildIfApplicable);
            }
            else //Assume library is a folder
            {
                library.Name = new DirectoryInfo(libraryLocation).Name;
                string binLocation = Path.Combine(libraryLocation, "bin");
                Console.WriteLine($"Loading library {library.Name}");
                
                string[] csprojs = Directory.GetFiles(libraryLocation, "*.csproj");
                if (csprojs.Length < 1)
                {
                    Utilities.WriteToConsoleWithColor($"No .csproj found at location {libraryLocation} . Please make sure you are specifying the immediate parent folder to a .csproj", ConsoleColor.Red);
                    return Enumerable.Empty<Type>();
                }

                library.Assembly = BuildCsprojAndLoadAssemblyFromBin(csprojs[0], forceRebuildIfApplicable);
            }

            IEnumerable<Type> tasksInDll = GetTypesOfTypeFromAssembly(library.Assembly, typeof(IClockworkTaskBase));

            if (!tasksInDll.Any())
            {
                Utilities.WriteToConsoleWithColor($"Library {library.Name} does not contain any tasks", ConsoleColor.Red);
            }

            return tasksInDll;
        }

        private static Assembly BuildCsprojAndLoadAssemblyFromBin(string csprojPath, bool forceRebuildIfApplicable = false)
        {
            string libraryLocation = new FileInfo(csprojPath).DirectoryName;
            string binLocation = Path.Combine(libraryLocation, "bin");
            string libraryName = Path.GetFileNameWithoutExtension(csprojPath);

            if (!Directory.Exists(binLocation) || forceRebuildIfApplicable)
            {
                var result = Utilities.RunProcess("dotnet", "build", libraryLocation);
                if (result.ExitCode != 0)
                {
                    Utilities.WriteToConsoleWithColor($"Build of library was unsuccessful. Log output below.\n\n{result.StdOut}\n\n{result.StdErr}", ConsoleColor.Red);
                    return null;
                }
                else if (!Directory.Exists(binLocation))
                {
                    Utilities.WriteToConsoleWithColor($"Build of library was successful, but bin folder was not found. Make sure the csproj does not have a different OutputPath specified", ConsoleColor.Red);
                    return null;
                }
            }

            string[] dlls = Directory.GetFiles(binLocation, $"{libraryName}.dll", SearchOption.AllDirectories);
            if (dlls.Length < 1)
            {
                Utilities.WriteToConsoleWithColor($"No {libraryName}.dll found. Make sure the csproj does not have a different OutputPath specified", ConsoleColor.Red);
                return null;
            }

            return LoadAssemblyFromDll(dlls[0]);
        }

        private static Assembly LoadAssemblyFromDll(string dllPath)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            string dllFolder = new FileInfo(dllPath).DirectoryName;
            var assemblies = Assembly.Load(File.ReadAllBytes(dllPath)); //Loading the assembly by its byte content means it doesn't stay loaded (so it can be reloaded later without unloading)

            //Load assembly dependencies
            foreach (AssemblyName refAssembly in assemblies.GetReferencedAssemblies())
            {
                string refAssemblyPath = Path.Combine(dllFolder, $"{refAssembly.Name}.dll");
                if (refAssembly.Name != "ClockworkFramework.Core" && File.Exists(refAssemblyPath))
                {
                    var assem = Assembly.Load(File.ReadAllBytes(refAssemblyPath));
                }
            }

            return assemblies;
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            return ((AppDomain)sender).GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
        }

        public static IEnumerable<MethodInfo> LoadExampleTasks()
        {
            IEnumerable<Type> exampleClasses = GetTypesOfTypeFromAssembly(Assembly.GetExecutingAssembly(), typeof(IClockworkTaskBase)).Where(t => t.Namespace == "ClockworkFramework.Examples");
            return exampleClasses.SelectMany(t => LoadTaskMethodsFromClassType(t));
        }

        //Todo: rework some of the below (loading TaskMethods *then* filtering to ones on IClockworkTaskBase classes) so we can output a warning
        //  for [TaskMethod]s that are on a class that doesn't inherit from IClockworkTaskBase
        public static IEnumerable<MethodInfo> LoadTaskMethodsFromClassType(Type type)
        {
            return type.GetMethods().Where(m => m.GetCustomAttribute(typeof(TaskMethodAttribute)) != null);
        }

        public static IEnumerable<Type> GetTypesOfTypeFromAssembly(Assembly assembly, Type type)
        {
            return assembly?.GetTypes().Where(t => type.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract) ?? Enumerable.Empty<Type>();
        }
    }
}