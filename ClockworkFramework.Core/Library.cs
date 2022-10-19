using System.Reflection;
using Newtonsoft.Json;

namespace ClockworkFramework.Core
{
    public class Library
    {
        public bool UpdateRepository { get; set; }

        public string Path { get; set; }

        [JsonIgnore]
        public Assembly Assembly { get; set; }

        [JsonIgnore]
        public string Name { get; set; } //Todo: need to assign


        //Todo: type? (dll, csproj, etc)
    }
}