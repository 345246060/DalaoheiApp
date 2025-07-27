using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using common;

namespace common
{
    public class CurlRequest
    {
        public string Url { get; set; }
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public string Cookies { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/json";
        public int TimeoutSeconds { get; set; } = 10;
    }
    /// <summary>
    /// 使用说明：
    /// 1. GET 请求示例：
    /// var request = new CurlRequest {
    ///     Url = "https://httpbin.org/get",
    ///     Method = "GET"
    /// };
    /// string result = await CurlHelper.ExecuteAsync(request);
    /// 
    /// 2. POST 请求 JSON 数据：
    /// var request = new CurlRequest {
    ///     Url = "https://httpbin.org/post",
    ///     Method = "POST",
    ///     Body = "{\"name\":\"Tom\"}"
    /// };
    /// JObject json = await CurlHelper.ExecuteJsonAsync(request);
    /// 
    /// 3. 添加 Headers 和 Cookies：
    /// var request = new CurlRequest {
    ///     Url = "https://httpbin.org/headers",
    ///     Headers = new Dictionary<string, string> {
    ///         {"X-Test", "demo"}
    ///     },
    ///     Cookies = "token=abc123; sessionid=xyz456"
    /// };
    /// 
    /// 4. 获取 JSON 中指定路径字段：
    /// string value = await CurlHelper.ExecuteJsonPathAsync(request, "headers.X-Test");
    /// </summary>
    public static class CurlHelper
    {
        private static readonly string InternalCurlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "curl.exe");

        public static Task<string> ExecuteAsync(CurlRequest request, string encodingName = "utf-8")
        {
            return Task.Factory.StartNew(() =>
            {
                string tempDataFile = null;
                try
                {
                    var sb = new StringBuilder();
                    sb.Append("--silent ");
                    sb.Append("--tlsv1.3 --compressed ");
                    if (request.Method.ToUpper() == "HEAD")
                    {
                        sb.Append("-i ");
                    } 
                    sb.Append("--max-time " + request.TimeoutSeconds + " ");
                    sb.Append("-X " + request.Method.ToUpper() + " ");
                    sb.Append("\"" + request.Url + "\" ");

                    //if (!string.IsNullOrWhiteSpace(request.ContentType))
                    //{
                    //    sb.AppendLine($"-H \"Content-Type: {request.ContentType}\" ");
                    //}

                    if (!string.IsNullOrWhiteSpace(request.Cookies))
                    {
                        sb.Append($"-b \"{request.Cookies.Replace("\"", "\\\"")}\" ");
                    }

                    foreach (var header in request.Headers)
                    {
                        string key = header.Key.Replace("\"", "\\\"");
                        string value = header.Value.Replace("\"", "\\\"");
                        sb.Append($"-H \"{key}: {value}\" ");
                    }

                    // 使用临时文件传 body
                    if (!string.IsNullOrWhiteSpace(request.Body) &&
                        (request.Method.ToUpper() == "POST" || request.Method.ToUpper() == "HEAD" || request.Method.ToUpper() == "PUT" || request.Method.ToUpper() == "PATCH"))
                    {
                        tempDataFile = Path.GetTempFileName();
                        File.WriteAllText(tempDataFile, request.Body, Encoding.UTF8);
                        sb.Append($"--data-raw @{tempDataFile} ");
                    }

                    return RunCurlCommand(sb.ToString(), encodingName);
                }
                catch (Exception ex)
                {
                    utility.logs("执行失败: " + utility.RemoveUrls(ex.Message));
                    return string.Empty;
                }
                finally
                {
                    // 清理临时文件
                    if (!string.IsNullOrEmpty(tempDataFile) && File.Exists(tempDataFile))
                    {
                        try { File.Delete(tempDataFile); } catch { }
                    }
                }
            });
        }

        public static Task<JObject> ExecuteJsonAsync(CurlRequest request)
        {
            return ExecuteAsync(request).ContinueWith(task =>
            {
                var response = task.Result;
                if (string.IsNullOrWhiteSpace(response)) return null;
                try
                {
                    return JObject.Parse(response);
                }
                catch (JsonReaderException)
                {
                    utility.logs("返回内容不是 JSON，原始数据如下：\n" + response);
                    return null;
                }
                catch (Exception ex)
                {
                    utility.logs("JSON解析失败: " + utility.RemoveUrls(ex.Message));
                    return null;
                }
            });
        }

        public static Task<T> ExecuteJsonAsync<T>(CurlRequest request)
        {
            return ExecuteAsync(request).ContinueWith(task =>
            {
                var response = task.Result;
                if (string.IsNullOrWhiteSpace(response)) return default(T);
                try
                {
                    return JsonConvert.DeserializeObject<T>(response);
                }
                catch (JsonReaderException)
                {
                    utility.logs("返回不是 JSON 格式，原始数据：\n" + response);
                    return default(T);
                }
                catch (Exception ex)
                {
                    utility.logs("反序列化失败: " + utility.RemoveUrls(ex.Message));
                    return default(T);
                }
            });
        }

        public static Task<string> ExecuteJsonPathAsync(CurlRequest request, string jsonPath)
        {
            return ExecuteJsonAsync(request).ContinueWith(task =>
            {
                var obj = task.Result;
                if (obj == null) return null;
                var token = obj.SelectToken(jsonPath);
                return token != null ? token.ToString() : null;
            });
        }

        private static string RunCurlCommand(string arguments, string encodingName = "utf-8")
        {
            try
            {
                string curlPath = File.Exists(InternalCurlPath) ? InternalCurlPath : "curl";
                var encoding = Encoding.GetEncoding(encodingName);
                var psi = new ProcessStartInfo
                {
                    FileName = curlPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = encoding,
                    StandardErrorEncoding = encoding
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(error) && !error.Contains("%"))
                    {
                        utility.logs("错误: " + error);
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                utility.logs("执行异常错误代码 007: " + utility.RemoveUrls(ex.Message));
                //请确保 curl.exe 已放置在 \"tools\\curl.exe\" 路径下，或系统已配置 curl 环境变量。\n下载地址：https://curl.se/windows/
                return string.Empty;
            }
        }
    }
}
