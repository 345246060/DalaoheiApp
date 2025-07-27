using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace common
{
    /// <summary>
    /// 公用方法类
    /// </summary>
    public class utility
    {
        private static readonly Random _random = new Random();

        private static readonly string[] knownMonitoringProcesses =
         {
            // 网络数据包捕获工具
            "Wireshark",     // 网络数据包抓取工具
            "tcpdump",       // 命令行数据包抓取工具
            "EtherApe",      // 图形化网络流量监控工具

            // 网络扫描工具
            "nmap",          // 网络端口扫描工具
            "Netcat",        // 网络通信工具
            "nc",            // Netcat 的简写
            "Zmap",          // 快速网络扫描器
            "Masscan",       // 大规模网络扫描工具

            // HTTP/HTTPS 调试代理工具
            "Fiddler",       // HTTP 调试代理
            "BurpSuite",     // Web 安全测试工具
            "mitmproxy",     // 中间人代理工具
            "Charles",       // Charles Proxy，用于 Web 调试
            "OWASP ZAP",     // OWASP ZAP，Web 安全测试工具

            // 入侵检测和防御系统
            "Snort",         // 网络入侵检测系统
            "Suricata",      // 高性能的入侵检测和防御系统
            "Bro",           // 网络安全监控和分析工具（现称 Zeek）

            // 安全分析工具
            "Metasploit",    // 渗透测试框架
            "Ettercap",      // 局域网嗅探和中间人攻击工具
            "Cain",          // 密码恢复和网络嗅探工具
            "Aircrack-ng",   // 无线网络破解工具
            "Kismet",        // 无线网络嗅探工具

            // 系统监控和调试工具 
            "Procmon",       // 进程监控工具（Process Monitor）
            "strace",        // Linux 下的系统调用追踪工具
            "DTrace",        // 动态跟踪工具，用于系统和应用监控

            // VPN 和代理工具（常用于掩盖网络流量）
            "OpenVPN",       // VPN 软件
            "SoftEtherVPN",  // VPN 工具
            "Shadowsocks",   // 代理工具
            "WireGuard",     // 现代VPN工具
            "Proxifier",     // 网络代理工具

            // 网络流量分析和监控
            "Netflow",       // 网络流量监控协议的实现工具
            "SolarWinds",    // 网络性能监控工具
            "Nagios",        // 服务器和网络监控工具
            "Zabbix",        // 开源监控工具

            // 其他
            "Zenmap",        // Nmap 的图形界面
            "Hping",         // 网络包分析和注入工具
            "Tcpreplay",     // 重放网络数据包工具
            "Scapy",         // 网络数据包操作和攻击工具
            "WiFite",        // 无线网络攻击自动化工具
        };
        // 检测是否有监听软件正在运行
        public static bool DetectMonitoringSoftware()
        {
            Process[] processList = Process.GetProcesses();
            foreach (Process process in processList)
            {
                if (knownMonitoringProcesses.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Detected monitoring software: {process.ProcessName}");
                    return true;
                }
            }
            return false;
        }
        // 删除目录
        public static bool ForceClearDirectory(string directoryPath, out string message)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    message = $"目录不存在：{directoryPath}";
                    return false;
                }

                var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal); // 清理只读
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // 如果文件被占用，尝试重命名再删除（适用于临时缓存）
                        string tempName = file + ".delete_" + Guid.NewGuid().ToString("N");
                        try
                        {
                            File.Move(file, tempName);
                            File.Delete(tempName);
                        }
                        catch (Exception ex2)
                        {
                            return Fail(file, ex2, out message);
                        }
                    }
                    catch (Exception ex)
                    {
                        return Fail(file, ex, out message);
                    }
                }

                // 可选：清空空子目录
                var dirs = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories);
                foreach (var dir in dirs)
                {
                    try { Directory.Delete(dir, true); } catch { /* 忽略 */ }
                }

                message = $"已清空目录：{directoryPath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"清空目录失败：{ex.Message}";
                return false;
            }
        }

        private static bool Fail(string file, Exception ex, out string message)
        {
            message = $"删除文件失败：{file}\n原因：{ex.Message}";
            return false;
        }
        /// <summary>
        /// 从 JSON 中提取 rtmp_push_url 并解码转义字符
        /// </summary>
        /// <param name="jsonStr">原始 JSON 字符串</param>
        /// <returns>清洗后的 RTMP 推流地址</returns>
        public static string ExtractRtmpPushUrl(string jsonStr)
        {
            try
            {
                JObject json = JObject.Parse(jsonStr);
                string rtmpUrl = json["data"]?["rtmp_push_url"]?.ToString();

                if (string.IsNullOrEmpty(rtmpUrl))
                {
                    return string.Empty;
                } 
                // 替换转义的 \u0026 为 &
                rtmpUrl = rtmpUrl.Replace("\\u0026", "&"); 
                return rtmpUrl;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        private static readonly Random random = new Random();

         
        /// <summary>
        /// 生成一个长度为 18 位的纯数字随机 ID（不限制前缀）
        /// </summary>
        public static string GenerateId()
        {
            var sb = new StringBuilder(18);
            var random = new Random(Guid.NewGuid().GetHashCode()); // 避免重复

            for (int i = 0; i < 18; i++)
            {
                sb.Append(random.Next(0, 10)); // 添加一个 0-9 的数字
            }

            return sb.ToString();
        }
        /// <summary>
        /// 计算从开播时间到现在的时长（小时，保留2位小数）
        /// </summary>
        /// <param name="startTimestamp">开播时间戳（支持毫秒或秒）</param>
        /// <returns>播放时长（小时）</returns>
        public static float GetLiveDurationHours(long startTimestamp)
        {
            try
            {
                // 将开播时间戳标准化为秒
                if (startTimestamp > 9999999999)
                    startTimestamp /= 1000;

                // 获取当前系统时间的 Unix 时间戳（秒）
                long nowTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (nowTimestamp < startTimestamp)
                    throw new ArgumentException("当前时间不能早于开播时间");

                long seconds = nowTimestamp - startTimestamp;
                float hours = seconds / 3600f;

                return (float)Math.Round(hours, 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine("计算开播时长失败：" + ex.Message);
                return 0f;
            }
        }
        /// <summary>
        /// 将 Unix 时间戳转换为 DateTime 对象（支持秒或毫秒级）。
        /// </summary>
        /// <param name="timestamp">Unix 时间戳（毫秒或秒）</param>
        /// <returns>北京时间（本地时间）</returns>
        public static DateTime ConvertUnixTimestamp(long timestamp)
        {
            try
            {
                // 判断是否为毫秒级
                if (timestamp > 9999999999)
                {
                    // 毫秒 → 秒
                    timestamp = timestamp / 1000;
                }

                // Unix 时间戳起点
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestamp);

                // 转为本地时间（含北京时间）
                return dateTimeOffset.LocalDateTime;
            }
            catch (Exception ex)
            {
                Console.WriteLine("时间戳转换失败：" + ex.Message);
                return DateTime.MinValue;
            }
        }
        /// <summary>
        /// 从程序根目录解压 ZIP 文件到当前目录，并覆盖已存在文件
        /// </summary>
        /// <param name="zipFileName">ZIP 文件名（仅文件名，不包含路径）</param>
        public static void ExtractZipWithOverwrite(string zipFileName)
        {
            try
            {
                // 获取程序运行的根目录
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string zipFilePath = Path.Combine(baseDirectory, zipFileName);

                if (string.IsNullOrWhiteSpace(zipFilePath) || !File.Exists(zipFilePath))
                {
                    Console.WriteLine("❌ ZIP 文件路径无效或文件不存在。");
                    return;
                }

                // 解压到程序根目录
                string extractPath = baseDirectory;

                // 解压 ZIP 文件到目标目录，覆盖已存在的文件
                using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(extractPath, entry.FullName);

                        // 如果是目录，继续
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                            continue;
                        }

                        // 确保目标目录存在
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                        // 覆盖已存在文件
                        entry.ExtractToFile(destinationPath, true);
                    }
                }

                Console.WriteLine("✅ 解压成功，所有文件已覆盖。");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("❌ 权限不足，请确保你有权限访问文件和目标文件夹。");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("❌ ZIP 文件未找到，请检查路径是否正确。");
            }
            catch (InvalidDataException)
            {
                Console.WriteLine("❌ ZIP 文件格式错误，无法解压。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 解压失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 在指定范围内生成一个随机整数值（包含最小值，包含最大值）
        /// </summary>
        /// <param name="min">最小值</param>
        /// <param name="max">最大值</param>
        /// <returns>随机整数值；如果输入错误，返回 -1</returns>
        public static int GetRandomInRange(int min, int max)
        {
            try
            {
                if (min > max)
                {
                    throw new ArgumentException("最小值不能大于最大值");
                }
                return _random.Next(min, max + 1); // 注意：max + 1 是为了包含 max 本身
            }
            catch (Exception ex)
            {
                Console.WriteLine("随机数生成失败：" + ex.Message);
                return -1; // 返回一个明显的错误值
            }
        }
        public static void logs(string text)
        {
            Console.WriteLine($"{text}");
        }
        // 更新 tabControl1 当前选中标签页的标题
        public static string Titleleng(string title, int leng)
        {
            try
            {
                // 判断标题是否超过25个字符，如果是则截取前25个字符并追加 "..."
                if (!string.IsNullOrEmpty(title) && title.Length > leng)
                {
                    title = title.Substring(0, leng) + "...";
                }
                return title;
            }
            catch (Exception ex)
            {
                return title;
            }
        }
        /// <summary>
        /// 过滤字符串中的所有链接（包括http/https、域名和IP地址）
        /// </summary>
        public static string RemoveUrls(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;

            // 包括 http/https/裸域名/IP 地址形式的链接
            string pattern = @"(https?:\/\/[^\s]+)|" +              // http/https 链接
                             @"(www\.[^\s]+)|" +                    // www.xxx.com 链接
                             @"((\d{1,3}\.){3}\d{1,3}(:\d+)?[^\s]*)"; // IP + 端口

            string result = Regex.Replace(input, pattern, "", RegexOptions.IgnoreCase);

            // 清理多余空格
            return Regex.Replace(result, @"\s+", " ").Trim();
        }
        /// <summary>
        /// 更具URL获取cookie信息
        /// </summary>
        /// <param name="webview"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<string> GetCookieHeaderForUrlAsync(WebView2 webview, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                Console.Error.WriteLine("url不能为空");

            try
            {
                if (webview.CoreWebView2 != null)
                {
                    var cookieManager = webview.CoreWebView2.CookieManager;
                    var cookies = await cookieManager.GetCookiesAsync(url); 
                    var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                    return cookieHeader;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"获取 URL({url}) 的 Cookie 时发生错误: {ex.Message}");
                return string.Empty;
            }
        }
        public static void buttonK(Button but)
        {
            but.Text = "处理中...";
            but.Enabled = false;
            but.BackColor = Color.LightGray;
        }
        public static void buttonE(Button but, string text, Color color)
        {
            but.Text = text;
            but.Enabled = true;
            but.BackColor = color;
        }
        /// <summary>
        /// 获取当天时间
        /// </summary>
        /// <returns></returns>
        public static int GetBeijingTimestamp()
        {
            // 获取当前UTC时间
            DateTime utcNow = DateTime.UtcNow;

            // 定义Unix时间的起始点（1970年1月1日）
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            // 计算自Unix Epoch以来的秒数
            int timestamp = (int)(utcNow - unixEpoch).TotalSeconds;

            return timestamp;
        }
        /// <summary>
        /// 过滤验证int类型
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static int setint(string val)
        {
            try
            {
               if (int.TryParse(val, out int intval))
                {
                    return intval;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public static decimal Set_decimal(string val)
        {
            try
            {
                if (decimal.TryParse(val, out decimal intval))
                {
                    return intval;
                }
                else
                {
                    return 0.00M;
                }
            }
            catch (Exception)
            {
                return 0.00M;
            }
        }
        public static double Set_double(string val)
        {
            try
            {
                if (double.TryParse(val, out double intval))
                {
                    return intval;
                }
                else
                {
                    return 0.00;
                }
            }
            catch (Exception)
            {
                return 0.00;
            }
        }
    }
    public class ret_model
    {
        public int code { get; set; }
        public string msg { get; set; }
        public dynamic data { get; set; }
        public int total { get; set; }
    }
}
