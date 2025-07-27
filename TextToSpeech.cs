using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosyVoiceClient
{
    public class TextToSpeech
    {
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";

        public TextToSpeech(string apiKey)
        {
            _apiKey = apiKey;
        }
        public async Task SynthesizeAsync(string text, string voice, string outputPath)
        {
            try
            {
                var payload = new
                {
                    model = "qwen-tts",
                    input = new
                    {
                        text = text,
                        voice = voice
                    }
                };

                var response = await _apiUrl
                    .WithHeader("Authorization", $"Bearer {_apiKey}")
                    .WithHeader("Content-Type", "application/json")
                    .PostJsonAsync(payload).ReceiveString();

                var body = response;
                var json = JObject.Parse(body);
                var audioUrl = json["output"]?["audio"]?["url"]?.ToString();

                if (string.IsNullOrWhiteSpace(audioUrl))
                {
                    Console.WriteLine("[ERROR] 响应中未包含音频 URL！");
                    Console.WriteLine(body);
                    return;
                }

                byte[] audioBytes = await audioUrl.GetBytesAsync();
                File.WriteAllBytes(outputPath, audioBytes);
                Console.WriteLine($"✅ 音频保存成功：{outputPath}");
            }
            catch (FlurlHttpException ex)
            {
                var errorBody = await ex.GetResponseStringAsync();
                Console.WriteLine($"[ERROR] 网络请求失败: {ex.Message}, 响应: {errorBody}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 未知错误: {ex.Message}");
            }
        }
    }
}