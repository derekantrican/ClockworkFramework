using System.Diagnostics;
using System.Runtime.InteropServices;
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
                int retries = 0;
                while (true)
                {
                    HttpResponseMessage response = client.Send(CreateRequest(parameters));
                    
                    int statusCode = (int)response.StatusCode;
                    if (statusCode >= 500 && statusCode < 600 && retries < 3) //Retry all 5XX response status codes
                    {
                        retries++;
                        Thread.Sleep(500 * retries);
                    }
                    else
                    {
                        return response;
                    }
                }
            }
        }

        private static HttpRequestMessage CreateRequest(ApiRequestParams parameters)
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

            return request;
        }

        public class ProcessResult
        {
            public string StdOut { get; set; }
            public string StdErr { get; set; }
            public int ExitCode { get; set; }
            public bool TimedOut { get; set; }
        }

        public static ProcessResult RunProcess(string executable, string arguments, string location = null, TimeSpan? timeout = null)
        {
            StringBuilder stdOutStringBuilder = new StringBuilder();
            StringBuilder stdErrStringBuilder = new StringBuilder();

            object stdOutSyncLock = new object();
            object stdErrSyncLock = new object();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                arguments = $"/c {executable} {arguments}";
                executable = "cmd.exe";
            }

            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
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

            try
            {
                p.Start();

                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                bool success = p.WaitForExit(timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : -1);

                if (!success)
                {
                    throw new TimeoutException();
                }
            }
            catch (Exception ex)
            {
                //Print out the exception (for debugging purposes) and rethrow
                Console.WriteLine(ex);
                throw; //Todo: call Hooks
            }


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