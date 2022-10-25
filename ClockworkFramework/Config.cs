using ClockworkFramework.Core;

namespace ClockworkFramework
{
    public class Config
    {
        public int RepositoryUpdateFrequency; //In minutes
        public List<Library> Libraries { get; set; } = new List<Library>();
    }
}