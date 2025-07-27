using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using common;
using System.Threading.Tasks;
using System.Linq;
using System.IO.Pipelines;

public class EnhancedFfmpegManager
{
    // 配置常量
    private const int MAX_RETRY_COUNT = 10;
    private const int HEALTH_CHECK_INTERVAL = 300000; // 5分钟
    private const int UA_ROTATION_INTERVAL = 3600000; // 1小时
    private const int TRAFFIC_VARIATION_CYCLE = 1800000; // 30分钟
    private const int STREAMKEY_REFRESH_INTERVAL = 10800000; // 3小时
    private const int MAX_AWAY_DURATION = 5; // 30分钟最大离开时间
    private const int MIN_AWAY_DURATION = 1; // 5分钟最小离开时间

    private readonly string ffmpegPath;
    private readonly string statusDir;
    private readonly string logDir;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> tokenMap = new ConcurrentDictionary<string, CancellationTokenSource>();
    private readonly ConcurrentDictionary<string, Thread> threadMap = new ConcurrentDictionary<string, Thread>();
    private readonly ConcurrentDictionary<string, FfmpegSessionState> sessionStates = new ConcurrentDictionary<string, FfmpegSessionState>();

    // 抖音直播伴侣UA池
    private string[] userAgents =
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) DouyinLive/5.8.0 Chrome/91.0.4472.124 Electron/13.6.6 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) DouyinLive/5.7.1 Chrome/89.0.4389.128 Electron/12.0.7 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) DouyinLive/5.6.3 Chrome/87.0.4280.141 Electron/11.4.3 Safari/537.36"
    };

    // 流量控制配置
    private readonly TrafficProfile[] trafficProfiles =
    {
        new TrafficProfile { Bitrate = 1500, Variation = 0.2, FrameDrop = 0.05 },
        new TrafficProfile { Bitrate = 2000, Variation = 0.15, FrameDrop = 0.03 },
        new TrafficProfile { Bitrate = 2500, Variation = 0.1, FrameDrop = 0.01 }
    };

    public EnhancedFfmpegManager()
    {
        ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        statusDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status");
        logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        Directory.CreateDirectory(statusDir);
        Directory.CreateDirectory(logDir);
    }

    public bool AddTask(string deviceId, string rtmpUrl, out string message)
    {
        if (threadMap.ContainsKey(deviceId))
        {
            message = $"设备 {deviceId} 推流任务已存在。";
            return false;
        }

        var cts = new CancellationTokenSource();
        tokenMap[deviceId] = cts;

        var state = new FfmpegSessionState
        {
            DeviceId = deviceId,
            RtmpUrl = rtmpUrl,
            CurrentTrafficProfile = trafficProfiles[new Random().Next(trafficProfiles.Length)],
            LastUaRotation = DateTime.Now,
            LastStreamKeyRefresh = DateTime.Now
        };

        sessionStates[deviceId] = state;

        Thread thread = new Thread(() => EnhancedStreamWorker(deviceId, cts.Token));
        thread.IsBackground = true;
        thread.Start();
        threadMap[deviceId] = thread;

        // 启动定时器
        StartTimersForDevice(deviceId);

        message = $"推流任务 {deviceId} 启动成功。";
        return true;
    }

    public bool RemoveTask(string deviceId, out string message)
    {
        if (!tokenMap.ContainsKey(deviceId))
        {
            message = $"任务 {deviceId} 不存在或已终止。";
            return false;
        }

        try
        {
            tokenMap[deviceId].Cancel();

            if (threadMap.ContainsKey(deviceId))
            {
                threadMap[deviceId].Join(2000);
                threadMap.TryRemove(deviceId, out _);
            }

            tokenMap.TryRemove(deviceId, out _);
            sessionStates.TryRemove(deviceId, out _);
            UpdateStatus(deviceId, "停止");
            message = $"任务 {deviceId} 停止成功。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"停止任务失败：{ex.Message}";
            return false;
        }
    }

    // ================= 主播离开功能 =================
    public void HostAway(string deviceId, int awayDurationMinutes)
    {
        if (sessionStates.TryGetValue(deviceId, out var state))
        {
            // 限制离开时间在合理范围内 (5-30分钟)
            int actualDuration = Math.Max(MIN_AWAY_DURATION, Math.Min(MAX_AWAY_DURATION, awayDurationMinutes));
            state.HostAwayUntil = DateTime.Now.AddMinutes(actualDuration);
            LogInfo(deviceId, $"主播离开，预计 {actualDuration} 分钟后返回");

            // 更新状态
            UpdateStatus(deviceId, "主播离开中", $"主播离开中，{actualDuration}分钟后返回");
        }
    }

    public void HostReturn(string deviceId)
    {
        if (sessionStates.TryGetValue(deviceId, out var state))
        {
            state.HostAwayUntil = null;
            LogInfo(deviceId, "主播已返回");
            UpdateStatus(deviceId, "运行中", "主播已返回");
        }
    }

    // ================= 推流码刷新功能 =================
    public void RefreshStreamKey(string deviceId)
    {
        if (sessionStates.TryGetValue(deviceId, out var state))
        {
            try
            {
                // 调用推流码接口
                string newStreamKey = GetNewStreamKeyFromAPI();

                if (!string.IsNullOrEmpty(newStreamKey))
                {
                    // 更新推流地址
                    state.RtmpUrl = newStreamKey;
                    state.LastStreamKeyRefresh = DateTime.Now;
                    LogInfo(deviceId, $"推流码已刷新: {newStreamKey.Substring(0, Math.Min(20, newStreamKey.Length))}...");

                    // 如果是直播中，需要重启推流
                    if (state.CurrentProcess != null && !state.CurrentProcess.HasExited)
                    {
                        state.CurrentProcess.Kill();
                        LogInfo(deviceId, "重启推流以应用新推流码");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(deviceId, $"刷新推流码失败: {ex.Message}");
            }
        }
    }

    // ================= 状态查询 =================
    public string GetStatus(string deviceId)
    {
        string path = Path.Combine(statusDir, $"status_{deviceId}.json");
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            var jObj = JObject.Parse(json);

            // 从 JSON 对象中获取 lastRefresh 字段
            string lastRefresh = jObj["lastRefresh"]?.Value<string>() ?? "N/A";
            bool isHostAway = jObj["isHostAway"]?.Value<bool>() ?? false;

            var filtered = new JObject
            {
                ["status"] = jObj["status"] ?? "",
                ["error"] = jObj["error"] ?? "",
                ["isHostAway"] = isHostAway,
                ["lastRefresh"] = lastRefresh
            };

            return JsonConvert.SerializeObject(filtered);
        }
        catch (Exception ex)
        {
            return $"{{\"status\": \"ERROR\", \"error\": \"状态文件解析失败: {ex.Message}\"}}";
        }
    }

    // ================= 核心推流逻辑 =================
    private void EnhancedStreamWorker(string deviceId, CancellationToken token)
    {
        int retryCount = 0;
        var state = sessionStates[deviceId];

        while (!token.IsCancellationRequested && retryCount < MAX_RETRY_COUNT)
        {
            // 检查主播是否离开
            if (CheckHostAwayStatus(deviceId, state))
            {
                Thread.Sleep(60000); // 每分钟检查一次
                continue;
            }

            // 检查是否需要刷新推流码（每3小时）
            CheckStreamKeyRefresh(deviceId, state);

            try
            {
                // 生成动态FFmpeg命令
                var ffmpegArgs = BuildDynamicFfmpegCommand(state);

                // 记录命令
                LogCommand(deviceId, ffmpegArgs);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = ffmpegArgs,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    state.CurrentProcess = process;

                    process.Start();
                    UpdateStatus(deviceId, "运行中");

                    // 读取错误输出
                    var errorOutput = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (token.IsCancellationRequested)
                    {
                        UpdateStatus(deviceId, "手动停止");
                        break;
                    }

                    if (process.ExitCode != 0)
                    {
                        retryCount++;
                        state.RetryCount = retryCount;
                        LogError(deviceId, $"推流异常退出 (代码:{process.ExitCode}): {errorOutput}");
                        UpdateStatus(deviceId, "错误", $"异常退出代码: {process.ExitCode}");

                        // 智能延迟
                        int delay = Math.Min(30000, retryCount * 5000);
                        Thread.Sleep(delay);
                    }
                    else
                    {
                        UpdateStatus(deviceId, "正常结束");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                retryCount++;
                state.RetryCount = retryCount;
                LogError(deviceId, $"严重异常: {ex}");
                UpdateStatus(deviceId, "错误", ex.Message);
                Thread.Sleep(10000);
            }
            finally
            {
                state.CurrentProcess = null;
            }
        }

        // 任务结束处理
        if (retryCount >= MAX_RETRY_COUNT)
        {
            UpdateStatus(deviceId, "失败", "超过最大重试次数");
        }

        // 从管理器中移除
        tokenMap.TryRemove(deviceId, out _);
        threadMap.TryRemove(deviceId, out _);
        sessionStates.TryRemove(deviceId, out _);
    }

    // ================= 辅助方法 =================
    private void StartTimersForDevice(string deviceId)
    {
        // 健康检查定时器
        new Timer(_ => CheckStreamHealth(deviceId), null, HEALTH_CHECK_INTERVAL, HEALTH_CHECK_INTERVAL);

        // 流量变化定时器
        new Timer(_ => RotateTrafficProfile(deviceId), null,
            TRAFFIC_VARIATION_CYCLE, TRAFFIC_VARIATION_CYCLE);

        // UA更新定时器
        new Timer(_ => UpdateUserAgent(deviceId), null,
            UA_ROTATION_INTERVAL, UA_ROTATION_INTERVAL);
    }

    private bool CheckHostAwayStatus(string deviceId, FfmpegSessionState state)
    {
        if (state.HostAwayUntil.HasValue && state.HostAwayUntil > DateTime.Now)
        {
            TimeSpan awayTime = state.HostAwayUntil.Value - DateTime.Now;
            int remainingMinutes = (int)Math.Ceiling(awayTime.TotalMinutes);

            UpdateStatus(deviceId, "主播离开中", $"主播离开中，{remainingMinutes}分钟后返回");
            return true;
        }
        else if (state.HostAwayUntil.HasValue)
        {
            // 主播返回
            state.HostAwayUntil = null;
            LogInfo(deviceId, "主播返回，继续推流");
            UpdateStatus(deviceId, "运行中", "主播已返回");
        }
        return false;
    }

    private void CheckStreamKeyRefresh(string deviceId, FfmpegSessionState state)
    {
        // 3小时刷新一次
        if ((DateTime.Now - state.LastStreamKeyRefresh).TotalHours >= 3)
        {
            RefreshStreamKey(deviceId);
        }
    }

    private void CheckStreamHealth(string deviceId)
    {
        try
        {
            FfmpegSessionState state;
            if (!sessionStates.TryGetValue(deviceId, out state))
                return;

            if (state == null || state.CurrentProcess == null || state.CurrentProcess.HasExited)
                return;

            // 发送心跳命令保持活跃
            if (state.CurrentProcess != null && !state.CurrentProcess.HasExited)
            {
                state.CurrentProcess.StandardInput.WriteLine(
                    "drawtext reinit 'text=%{localtime\\\\:%H\\\\:%M\\\\:%S}'");
            }
        }
        catch (Exception ex)
        {
            LogError(deviceId, $"健康检查失败: {ex.Message}");
        }
    }

    private void RotateTrafficProfile(string deviceId)
    {
        FfmpegSessionState state;
        if (!sessionStates.TryGetValue(deviceId, out state))
            return;

        // 随机切换到不同的流量配置
        var newProfile = trafficProfiles[new Random().Next(trafficProfiles.Length)];
        state.CurrentTrafficProfile = newProfile;
        LogInfo(deviceId, $"切换到流量配置: {newProfile.Bitrate}kbps ±{newProfile.Variation * 100}%");
    }

    private void UpdateUserAgent(string deviceId)
    {
        FfmpegSessionState state;
        if (!sessionStates.TryGetValue(deviceId, out state))
            return;

        // 每1小时更换UA
        state.LastUaRotation = DateTime.Now;
        state.CurrentUserAgent = userAgents[new Random().Next(userAgents.Length)];
        LogInfo(deviceId, $"更新UserAgent: {state.CurrentUserAgent.Substring(0, 30)}...");
    }

    private string BuildDynamicFfmpegCommand(FfmpegSessionState state)
    {
        var profile = state.CurrentTrafficProfile;
        int baseBitrate = profile.Bitrate;

        // 添加随机波动
        int bitrateVariation = (int)(baseBitrate * (new Random().NextDouble() * profile.Variation));
        int actualBitrate = new Random().Next() % 2 == 0
            ? baseBitrate + bitrateVariation
            : baseBitrate - bitrateVariation;

        // 生成UA
        string userAgent = GetCurrentUserAgent(state);

        // 构建基础命令
        var cmd = new StringBuilder();

        // 视频源
        cmd.Append($"-f lavfi -i \"color=c=black:size=1280x720:r=30,");
        cmd.Append($"noise=all=100:allf=t:seed=floor(t*30),");
        cmd.Append($"drawtext=text='%{{localtime\\\\:%H\\\\:%M\\\\:%S}}':fontsize=12:fontcolor=white@0.01:x=10:y=10\" ");

        // 音频源
        cmd.Append($"-f lavfi -i \"anoisesrc=d=0:r=44100:a=0.001\" ");

        // 视频编码
        cmd.Append($"-c:v libx264 -preset ultrafast -tune zerolatency ");
        cmd.Append($"-b:v {actualBitrate}k -maxrate {actualBitrate + 500}k -bufsize {actualBitrate * 2}k ");
        cmd.Append($"-g 120 -keyint_min 120 -x264opts no-scenecut ");

        // 修复 -vf 滤镜括号闭合错误，使用合法语法
        int frameDropInterval = Math.Max(1, (int)(1 / profile.FrameDrop));
        cmd.Append($"-vf \"fps=30,select='not(mod(n\\,{frameDropInterval}))'\" ");

        // 音频编码
        cmd.Append($"-c:a aac -b:a 32k ");

        // 输出格式
        cmd.Append($"-f flv ");

        // UA伪装，空则默认设置一个UA，防止非法空值
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            userAgent = "DouyinLive/5.8.0 (Windows NT 10.0; Win64; x64)";
        }
        cmd.Append($"-user_agent \"{userAgent}\" ");

        // 自定义头部（必须每行 \r\n 结尾，否则部分平台会报错）
        cmd.Append($"-headers \"User-Agent: {userAgent}\\r\\n");
        cmd.Append($"X-TT-Env: live_windows\\r\\n");
        cmd.Append($"X-TT-TraceID: {Guid.NewGuid()}\\r\\n\" ");

        // 推流地址
        cmd.Append($"\"{state.RtmpUrl}\"");

        string test = cmd.ToString();

        return cmd.ToString();
    }

    private string GetCurrentUserAgent(FfmpegSessionState state)
    {
        // 每1小时或失败后更换UA
        if ((DateTime.Now - state.LastUaRotation).TotalHours >= 1 ||
            state.RetryCount > state.LastUaRotationRetryCount)
        {
            state.LastUaRotation = DateTime.Now;
            state.LastUaRotationRetryCount = state.RetryCount;
            state.CurrentUserAgent = userAgents[new Random().Next(userAgents.Length)];
        }
        return state.CurrentUserAgent;
    }

    private void UpdateStatus(string deviceId, string status, string error = "")
    {
        try
        {
            FfmpegSessionState state;
            bool isHostAway = sessionStates.TryGetValue(deviceId, out state) &&
                              state.HostAwayUntil.HasValue &&
                              state.HostAwayUntil > DateTime.Now;

            var obj = new
            {
                device_id = deviceId,
                status = status,
                error = error,
                timestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                isHostAway = isHostAway,
                lastRefresh = state?.LastStreamKeyRefresh.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"
            };

            string path = Path.Combine(statusDir, $"status_{deviceId}.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
        catch (Exception ex)
        {
            LogError(deviceId, $"更新状态失败: {ex.Message}");
        }
    }

    private void LogError(string deviceId, string message)
    {
        try
        {
            string path = Path.Combine(logDir, $"error_{deviceId}.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\r\n");
        }
        catch { /* 忽略日志错误 */ }
    }

    private void LogInfo(string deviceId, string message)
    {
        try
        {
            string path = Path.Combine(logDir, $"info_{deviceId}.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\r\n");
        }
        catch { /* 忽略日志错误 */ }
    }

    private void LogCommand(string deviceId, string command)
    {
        try
        {
            string path = Path.Combine(logDir, $"cmd_{deviceId}.log");
            File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CMD: {command}\r\n");
        }
        catch { /* 忽略日志错误 */ }
    }

    // ================= 推流码接口（需实现） =================
    private string GetNewStreamKeyFromAPI()
    {
        try
        {
            string cookie_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", GlobalData.deviceId);
            string cookies = CookieStorage.ReadCookie(cookie_path + "cookie.txt");

            var ret = douyin_api.GetLatestRoomInfoAsync(cookies, GlobalData.deviceId, GlobalData.iid, GlobalData.short_id)
                                .GetAwaiter().GetResult();

            if (ret != null)
            {
                GlobalData.stream_id = ret.stream_id;
                GlobalData.room_id = ret.room_id;
                GlobalData.rtmp_push_url = ret.rtmp_push_url;
                return ret.rtmp_push_url;
            }
            else
            {
                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            // 异常处理，防止卡死
            Console.WriteLine("拉取推流码失败：" + ex.Message);
            return string.Empty;
        }
    }

    // ================= 状态类 =================
    private class FfmpegSessionState
    {
        public string DeviceId { get; set; }
        public string RtmpUrl { get; set; }
        public Process CurrentProcess { get; set; }
        public TrafficProfile CurrentTrafficProfile { get; set; }
        public string CurrentUserAgent { get; set; }
        public DateTime LastUaRotation { get; set; }
        public int LastUaRotationRetryCount { get; set; }
        public int RetryCount { get; set; }
        public DateTime LastStreamKeyRefresh { get; set; }
        public DateTime? HostAwayUntil { get; set; }
    }

    private class TrafficProfile
    {
        public int Bitrate { get; set; }         // 基础码率 (kbps)
        public double Variation { get; set; }     // 波动范围 (0-1)
        public double FrameDrop { get; set; }     // 丢帧比例 (0-1)
    }

 
}
