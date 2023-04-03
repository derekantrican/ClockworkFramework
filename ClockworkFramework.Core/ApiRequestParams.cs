
namespace ClockworkFramework.Core
{
    public class ApiRequestParams
    {
        public string Url { get; set; }
        public HttpMethod Method { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public HttpContent Content { get; set; }
    }
}