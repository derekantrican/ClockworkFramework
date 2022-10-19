using ClockworkFramework.Core;

namespace Clockwork
{
    public class Config
    {
        public int RepositoryUpdateFrequency; //In minutes
        public List<Library> Libraries { get; set; } = new List<Library>();
    }
}