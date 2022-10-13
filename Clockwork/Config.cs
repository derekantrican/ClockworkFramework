namespace Clockwork
{
    public class Config
    {
        public class Library
        {
            public bool UpdateRepository { get; set; }
            public string Path { get; set; }
        }

        public List<Library> Libraries { get; set; } = new List<Library>();
    }
}