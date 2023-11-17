using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;

namespace ClockworkFramework.Core
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
            //Note that this may not work 100% because of the asynchronous nature of tasks
            Console.ForegroundColor = color;
            Console.WriteLine(obj);
            Console.ResetColor();
        }

        public static string ApiRequest(string url, HttpMethod method, Dictionary<string, string> headers = null,
                                        Dictionary<string, string> parameters = null, HttpContent content = null) 
        {
            var response = ApiRequest(new ApiRequestParams
            {
                Url = url,
                Method = method,
                Headers = headers,
                Parameters = parameters,
                Content = content,
            });

            using (var stream = response.Content.ReadAsStream())
            {
                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        public static HttpResponseMessage ApiRequest(ApiRequestParams parameters)
        {
            using (var client = new HttpClient())
            {
                string url = parameters.Url;
                if (parameters.Parameters != null)
                {
                    url += $"?{string.Join("&", parameters.Parameters.Select(param => $"{param.Key}={Uri.EscapeDataString(param.Value)}"))}";
                }

                var request = new HttpRequestMessage(parameters.Method, url);
                if (parameters.Headers != null)
                {
                    foreach (var header in parameters.Headers)
                    {
                        request.Headers.Add(header.Key, header.Value);
                    }
                }

                request.Content = parameters.Content;

                return client.Send(request);
            }
        }

        public class ProcessResult
        {
            public string StdOut { get; set; }
            public string StdErr { get; set; }
            public int ExitCode { get; set; }
        }

        public static ProcessResult RunProcess(string process, string location = null)
        {
            StringBuilder stdOutStringBuilder = new StringBuilder();
            StringBuilder stdErrStringBuilder = new StringBuilder();

            object stdOutSyncLock = new object();
            object stdErrSyncLock = new object();

            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {process}",
                    WorkingDirectory = location,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                },
            };

            p.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    lock (stdOutSyncLock)
                    {
                        stdOutStringBuilder.AppendLine(args.Data);
                    }
                }
            };

            p.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    lock (stdErrSyncLock)
                    {
                        stdErrStringBuilder.AppendLine(args.Data);
                    }
                }
            };

            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            return new ProcessResult
            {
                StdOut = stdOutStringBuilder.ToString(),
                StdErr = stdErrStringBuilder.ToString(),
                ExitCode = p.ExitCode,
            };
        }

        public static void RunWithCatch(Action action, Action<Exception> onException)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                onException?.Invoke(e);
            }
        }

        public static T RunWithCatch<T>(Func<T> action, Func<Exception, T> onException)
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                return onException != null ? onException(e) : default(T);
            }
        }

        public static void ProcessFullException(Exception ex, Action<Exception> exceptionAction)
        {
            exceptionAction?.Invoke(ex);

            if (ex is AggregateException aggregateException)
            {
                foreach (Exception innerException in aggregateException.InnerExceptions)
                {
                    ProcessFullException(innerException, exceptionAction);
                }
            }
            else if (ex.InnerException != null)
            {
                ProcessFullException(ex.InnerException, exceptionAction);
            }
        }
    }
}