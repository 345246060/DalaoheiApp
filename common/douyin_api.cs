using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using common;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace common
{
 
    public class CookieItem
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class ApiResponse
    {
        public int code { get; set; }
        public List<CookieItem> data { get; set; }
    }
    public class bind_goods
    {
        public string promotion_id { get; set; }
        public string bind_source { get; set; }
        public string product_id { get; set; }
        public string item_type { get; set; }
    }
    public class goods_live
    {
        public string outer_id { get; set; }
        public string secret_key { get; set; }
        public List<bind_goods> bind_goods { get; set; }
    }
    /// <summary>
    /// 创建优惠券参数实体类
    /// </summary>
    public class create_quan
    {
        public string outer_id { get; set; }
        public string secret_key { get; set; }
        public string juan_time { get; set; }
        public string credit { get; set; }
        public string total_amount { get; set; }
        public List<string> promotion_ids { get; set; }
        public string goods_id_list { get; set; }
    }
    public class zhibopar
    {
        public string room_id { get; set; }
        public string stream_id { get; set; }
        public string short_id { get; set; }
        public string nickname { get; set; }
        public string rtmp_push_url { get; set; }
        public string start_time { get; set; }
    }
    /// <summary>
    /// 抖音API接口类
    /// </summary>
    public class douyin_api
    {
        //static string ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36  (KHTML, like Gecko) Chrome/91.0.4472.77 Safari/537.36";

        public static IDictionary<string, string> ParseCookieString(string cookieString)
        {
            var dict = new Dictionary<string, string>();
            var pairs = cookieString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var parts = pair.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    dict[key] = value;
                }
            }
            return dict;
        }
        static string app_name = "livepro";
        static string version_code = "30.3.0";
        static string webcast_sdk_version = "3450";
        static string device_platform = "iphone";
        static string channel = "AppStore";
        static string os_version = "18.1.1";
        static string language = "zh-Hans-US";
        static string aid = "547599";
        /// <summary>
        /// 获取直播间数据
        /// </summary>
        /// <param name="cookies"></param>
        /// <param name="deviceId"></param>
        /// <param name="iid"></param>
        /// <returns></returns>
        public static async Task<zhibopar> GetLatestRoomInfoAsync(string cookies, string deviceId, string iid,string _short_id)
        {
            try
            {
                string url = $"https://webcast.amemv.com/webcast/room/get_latest_room/" +
                             $"?ac=wifi&app_name=com.dji.golite&version_code=1.17.1" +
                             $"&device_platform=iphone&webcast_sdk_version=20004" +
                             $"&resolution=12x32&os_version=10.0.19043&language=zh" +
                             $"&aid=2079&live_id=1&channel=online&device_id={deviceId}&iid={iid}";
                //string url = $"https://webcast.amemv.com/webcast/room/get_latest_room/" +
                //             $"?ac=wifi&app_name={app_name}&version_code={version_code}" +
                //             $"&device_platform={device_platform}&webcast_sdk_version={webcast_sdk_version}" +
                //             $"&resolution=720x1280&os_version={os_version}&language={language}" +
                //             $"&aid={aid}&live_id=1&channel={channel}&device_id={deviceId}&iid={iid}";

                // 构造 curl 参数（加 --silent 禁止进度条输出）
                string curlArgs = $"--silent -X POST \"{url}\" " +
                                  "-H \"Connection: keep-alive\" " +
                                  "-H \"User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0\" " +
                                  "-H \"Host: webcast.amemv.com\" " +
                                  "-H \"Content-Type: application/json\" " +
                                  "-H \"Cookie: " + cookies.Replace("\"", "\\\"") + "\" " +
                                  "--data \"{}\"";

                string result = await RunCurlCommandAsync(curlArgs);
                if (string.IsNullOrWhiteSpace(result))
                {
                    utility.logs("调用失败，未返回内容");
                    return null;
                }

                JObject json = JObject.Parse(result);
                string roomId = json["data"]?["id"]?.ToString();
                string streamId = json["data"]?["stream_id"]?.ToString();
                string short_id = json.SelectToken("data.owner.short_id")?.ToString(); 
                string nickname = json.SelectToken("data.owner.nickname")?.ToString();
                string rtmp_push_url = json.SelectToken("data.stream_url.rtmp_push_url")?.ToString(); // 推流码
                
                if (string.IsNullOrWhiteSpace(short_id))
                {
                    utility.logs("未获取到有效直播数据");
                    return null;
                }
                string start_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // 回传直播信息
                var data = new
                {
                    deviceId = deviceId,
                    nickname = nickname,
                    short_id = short_id,
                    start_time = start_time,
                    room_id = roomId,
                    stream_id = streamId
                };
 
                return new zhibopar
                {
                    room_id = roomId,
                    stream_id = streamId,
                    short_id = short_id,
                    start_time = start_time,
                    nickname = nickname,
                    rtmp_push_url = rtmp_push_url
                };
            }
            catch (Exception ex)
            {
                utility.logs("获取直播信息失败：" + ex.Message);
                return null;
            }
        }
        public static async Task<string> RunCurlCommandAsync(string arguments)
        {
            string curlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "curl.exe");
            var psi = new ProcessStartInfo
            {
                FileName = curlPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = Process.Start(psi))
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                process.WaitForExit(); 
                if (!string.IsNullOrWhiteSpace(error) && !error.Contains("%"))
                {
                    utility.logs("错误：" + error);
                }

                return output;
            }
        }
 
        /// <summary>
        /// 定时心跳
        /// </summary>
        /// <param name="cookies"></param>
        /// <param name="stream_id"></param>
        /// <param name="room_id"></param>
        /// <param name="status"></param>
        /// <param name="device_id"></param>
        /// <param name="iid"></param>
        /// <returns></returns>
        public static async Task<ret_model> ping_anchor(string cookies, string stream_id, string room_id, string status, string device_id, string iid)
        {
            try
            {
                string url = $"https://webcast.amemv.com/webcast/room/ping/anchor/" +
                             $"?ac=wifi&app_name=com.dji.golite&version_code=1.17.1" +
                             $"&device_platform=iphone&webcast_sdk_version=20004" +
                             $"&resolution=12x32&os_version=10.0.19043&language=zh&aid=2079&live_id=1" +
                             $"&channel=online&device_id={device_id}&iid={iid}";

                //string url = $"https://webcast.amemv.com/webcast/room/ping/anchor/" +
                //             $"?ac=wifi&app_name={app_name}&version_code={version_code}" +
                //             $"&device_platform={device_platform}&webcast_sdk_version={webcast_sdk_version}" +
                //             $"&resolution=720x1280&os_version={os_version}&language={language}&aid={aid}&live_id=1" +
                //             $"&channel={channel}&device_id={device_id}&iid={iid}";

                // 构造 post 数据（URL 编码格式）
                string postData = $"stream_id={stream_id}&room_id={room_id}&status={status}";

                string curlArgs = $"--silent -X POST \"{url}\" " +
                                  "-H \"Connection: keep-alive\" " +
                                  "-H \"User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0\" " +
                                  "-H \"Host: webcast.amemv.com\" " +
                                  "-H \"Content-Type: application/x-www-form-urlencoded\" " +
                                  "-H \"Cookie: " + cookies.Replace("\"", "\\\"") + "\" " +
                                  $"--data \"{postData}\"";

                string result = await RunCurlCommandAsync(curlArgs);
                if (string.IsNullOrWhiteSpace(result))
                {
                    utility.logs("心跳请求失败：无返回");
                    return new ret_model { code = -1, msg = "心跳失败，服务器无响应" };
                }

                JObject json = JObject.Parse(result);
                string status_code = json.SelectToken("status_code")?.ToString();
                string prompts = json.SelectToken("data.prompts")?.ToString() ?? "未知状态";
                string message = json.SelectToken("data.message")?.ToString() ?? "";

                if (status_code == "0")
                {
                    return new ret_model { code = 0, msg = "正在直播", data = message };
                }
                else
                {
                    DeviceQueueManager.Instance.EnqueueDevice(device_id);
                    return new ret_model { code = -1, msg = prompts, data = prompts }; 
                }
            }
            catch (Exception ex)
            {
                utility.logs("错误：" + ex.Message);
                return new ret_model { code = -1, msg = "请求异常：" + ex.Message };
            }
        } 
    }
}
