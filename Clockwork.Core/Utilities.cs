using Newtonsoft.Json;

namespace Clockwork.Core
{
    public class Utilities
    {
        public static T LoadOrCreateData<T>(string filePath, T defaultValue)
        {
            try
            {
                return LoadData<T>(filePath);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static T LoadData<T>(string filePath)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(filePath));
        }

        public static void SaveData<T>(string filePath, T data)
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(data));
        }

        public static dynamic JsonToDynamic(string json)
        {
            return JsonConvert.DeserializeObject<dynamic>(json);
        }

        public static void WriteToConsoleWithColor(object obj, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(obj);
            Console.ResetColor();
        }

        public static string ApiRequest(string url, HttpMethod method, Dictionary<string, string> headers = null /*Todo: also need params & content*/) 
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(method, url);
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                var response = client.Send(request);
                using (var stream = response.Content.ReadAsStream())
                {
                    using (var streamReader = new StreamReader(stream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}