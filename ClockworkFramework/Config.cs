namespace Clockwork
{
    public class Config
    {
        public int RepositoryUpdateFrequency; //In minutes
        public List<Library> Libraries { get; set; } = new List<Library>();

        public class Library
        {
            public bool UpdateRepository { get; set; }
            public string Path { get; set; }
        }
    }
}