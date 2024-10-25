
namespace ClockworkFramework.Core
{
    public class DependencyFileAttribute : Attribute
    {
        public string File { get; set; }
        public DependencyFileAttribute(string file)
        {
            File = file;
        }
    }
}