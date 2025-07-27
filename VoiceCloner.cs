using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json.Linq;

namespace CosyVoiceClient
{
    public class VoiceCloner
    {
        private readonly string _apiKey;
        private readonly string _voiceApiUrl = "https://dashscope.aliyuncs.com/api/v1/services/aigc/voice-cloning/voices";

        public VoiceCloner(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string> RegisterVoiceAsync(string audioFilePath, string voiceName)
        {
            try
            {
                var response = await _voiceApiUrl
                    .WithHeader("Authorization", "Bearer " + _apiKey)
                    .WithHeader("X-DashScope-Async", "enable")
                    .PostMultipartAsync(content =>
                    {
                        content.AddString("name", voiceName);
                        content.AddFile("file", audioFilePath, "audio/wav");
                    }).ReceiveJson();

                var statusCode = response.StatusCode;
                var responseBody = await response.GetStringAsync();

                if (statusCode >= 200 && statusCode < 300)
                {
                    Logger.Info("音色注册成功！");
                    Logger.Info(responseBody);

                    return ExtractVoiceIdFromResponse(responseBody);
                }
                else
                {
                    Logger.Error($"注册失败: {statusCode}, 错误信息: {responseBody}");
                    return null;
                }
            }
            catch (FlurlHttpException ex)
            {
                string errorBody = null;
                try
                {
                    errorBody = await ex.GetResponseStringAsync();
                }
                catch { }

                Logger.Error($"网络请求异常: {ex.Message}, 响应内容: {errorBody}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("系统异常：" + ex.Message);
                return null;
            }
        }

        private string ExtractVoiceIdFromResponse(string json)
        {
            try
            {
                var obj = JObject.Parse(json);
                var voiceId = obj["result"]?["voice_id"]?.ToString();
                return voiceId;
            }
            catch (Exception ex)
            {
                Logger.Error("解析响应失败: " + ex.Message);
                return null;
            }
        }
    }

    // 你应当实现这个 Logger 类，以下是简单示例：
    public static class Logger
    {
        public static void Info(string msg) => Console.WriteLine("[INFO] " + msg);
        public static void Error(string msg) => Console.WriteLine("[ERROR] " + msg);
    }
}