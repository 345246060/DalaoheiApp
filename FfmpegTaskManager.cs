using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using common;

public class FfmpegTaskManager
{
    private readonly string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
    private readonly string statusDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "status");
    private readonly string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

    private readonly ConcurrentDictionary<string, CancellationTokenSource> tokenMap = new ConcurrentDictionary<string, CancellationTokenSource>();
    private readonly ConcurrentDictionary<string, Thread> threadMap = new ConcurrentDictionary<string, Thread>();
    private readonly ConcurrentDictionary<string, Process> processMap = new ConcurrentDictionary<string, Process>();

    public FfmpegTaskManager()
    {
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

        Thread thread = new Thread(() => StreamWorker(deviceId, rtmpUrl, cts.Token));
        thread.IsBackground = true;
        thread.Start();
        threadMap[deviceId] = thread;

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

            // Kill FFmpeg 进程（如果存在）
            if (processMap.TryRemove(deviceId, out var process))
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        Console.WriteLine($"[结束] 已强制关闭 {deviceId} PID: {process.Id}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[警告] 关闭 {deviceId} 异常: {ex.Message}");
                }
            }

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

    public string GetStatus(string deviceId)
    {
        string path = Path.Combine(statusDir, $"status_{deviceId}.json");
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            var jObj = JObject.Parse(json);
            var filtered = new JObject
            {
                ["状态"] = jObj["status"] ?? "",
                ["error"] = jObj["error"] ?? ""
            };
            return JsonConvert.SerializeObject(filtered);
        }
        catch (Exception ex)
        {
            return $"{{\"状态\": \"ERROR\", \"error\": \"状态文件解析失败: {ex.Message}\"}}";
        }
    }

    public List<string> GetAllTaskIds()
    {
        return new List<string>(threadMap.Keys);
    } 
    private void TryKillProcess(Process process)
    {
        if (process == null)
            return;
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
                if (!process.WaitForExit(3000))
                {
                    // 如果 3 秒还没退出，可以再补一刀
                    try
                    {
                        Process p2 = Process.GetProcessById(process.Id);
                        if (!p2.HasExited)
                        {
                            p2.Kill();
                            p2.WaitForExit(1000);
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }
    }
    private void StreamWorker(string deviceId, string rtmpUrl, CancellationToken token)
    {
        int retryCount = 0;
        int maxRetry = 5;
        int processTimeoutSec = 120;

        try
        {
            while (!token.IsCancellationRequested)
            {
                UpdateStatus(deviceId, "启动", "启动成功。。。");

                Process process = null;
                bool shouldRetry = false;
                DateTime lastFrameUpdate = DateTime.Now;

                try
                {
                    string filter = "[0:v]drawbox=c=black@1:t=fill," +
                                "noise=alls=20:allf=t," +
                                "boxblur=2," +
                                "eq=contrast='if(lt(mod(t\\,5)\\,2)\\,1.03\\,1)'," +
                                "eq=brightness='if(lt(mod(t\\,8)\\,3)\\,-0.02\\,0)',";

                    for (int i = 1; i <= 20; i++)
                    {
                        string y = $"h*{(0.05 + (0.9 - 0.05) / 19 * (i - 1)):0.###}";
                        int speed = 60 + (i % 5) * 10;

                        filter +=
                            $"drawtext=fontfile=simhei.ttf:" +
                            $"textfile='danmu{i}.read.txt':reload=1:fontcolor=dimgray@0.5:fontsize=30:" +
                            $"x='w - mod(t*{speed}\\, w+text_w)':y='{y}',";
                    }

                    filter = filter.TrimEnd(',') + "[vout];" +
                             "[1:a][2:a]concat=n=2:v=0:a=1,aloop=loop=-1:size=1323000[beep];" +
                             "[beep][3:a]amix=inputs=2:duration=longest:dropout_transition=2,volume=0.0003[aout]";

                    string ffmpegCmd =
                        "-y -re " +
                        "-f lavfi -i color=c=black:s=12x32:r=15 " +
                        "-f lavfi -i sine=frequency=800:duration=0.5 " +
                        "-f lavfi -i aevalsrc=0:d=29.5 " +
                        "-f lavfi -i anoisesrc=c=pink:r=44100 " +
                        "-filter_complex \"" + filter + "\" " +
                        "-map \"[vout]\" -map \"[aout]\" " +
                        "-c:v libx264 -preset veryfast -b:v 10k -maxrate 10k -bufsize 10k -g 10 -pix_fmt yuv420p " +
                        "-c:a aac -b:a 96k -ar 22050 -ac 1 " +
                        "-f flv " +
                        "-user_agent \"DJI Fly/1109 CFNetwork/1568.200.51 Darwin/24.1.0\" " +
                        "-metadata title=\"直播间\" " +
                        "-rtmp_live live \"" + rtmpUrl + "\"";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = ffmpegCmd,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    process = new Process { StartInfo = psi };
                    process.Start();
                    processMap[deviceId] = process;
                    UpdateStatus(deviceId, "推流中", "正常");

                    StringBuilder errSb = new StringBuilder();
                    Thread stderrThread = new Thread(() =>
                    {
                        try
                        {
                            string line;
                            while ((line = process.StandardError.ReadLine()) != null)
                            {
                                errSb.AppendLine(line);
                                if (line.Contains("frame="))
                                {
                                    lastFrameUpdate = DateTime.Now;
                                }
                            }
                        }
                        catch { }
                    });
                    stderrThread.IsBackground = true;
                    stderrThread.Start();

                    int checkInterval = 5000;
                    int elapsed = 0;
                    while (!process.HasExited && !token.IsCancellationRequested)
                    {
                        Thread.Sleep(checkInterval);
                        elapsed += checkInterval;

                        if ((DateTime.Now - lastFrameUpdate).TotalSeconds > processTimeoutSec)
                        {
                            TryKillProcess(process);
                            retryCount++;
                            shouldRetry = true;
                            LogError(deviceId, "日志长时间无frame输出，Kill并重启\n" + errSb);
                            UpdateStatus(deviceId, "错误", "日志无frame输出，疑似卡死自动重启。。。" + retryCount);
                            break;
                        }
                    }

                    stderrThread.Join(1000);

                    if (token.IsCancellationRequested)
                    {
                        UpdateStatus(deviceId, "已取消");
                        break;
                    }

                    if (process.HasExited && process.ExitCode != 0)
                    {
                        TryKillProcess(process);
                        retryCount++;
                        shouldRetry = true;
                        LogError(deviceId, $"推流异常退出（ExitCode={process.ExitCode}）：{errSb}");
                        UpdateStatus(deviceId, "错误", errSb.ToString());
                    }
                    else if (process.HasExited)
                    {
                        UpdateStatus(deviceId, "已结束");
                        retryCount = 0;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    TryKillProcess(process);
                    retryCount++;
                    shouldRetry = true;
                    LogError(deviceId, $"异常：{ex.Message}");
                    UpdateStatus(deviceId, "错误", ex.Message);
                }

                if (retryCount >= maxRetry)
                {
                    UpdateStatus(deviceId, "错误", $"连续失败{maxRetry}次，会自动动重启。。。忽略我");
                    retryCount = 0;
                    string status = GetStatus(deviceId);
                    Console.WriteLine(status);
                    if (status.Contains("直播已结束"))
                    {
                        break;
                    }
                }

                if (shouldRetry)
                {
                    int delay = Math.Min(3000 + 2000 * retryCount, 15000);
                    LogError(deviceId, $"推流重启前等待 {delay / 1000} 秒，避免IO锁死...");
                    UpdateStatus(deviceId, "推流中", $"推流重启前等待 {delay / 1000} 秒，避免IO锁死..."); 
                    Thread.Sleep(delay);
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            LogError(deviceId, "外部异常: " + ex);
            UpdateStatus(deviceId, "错误", ex.ToString());
        }
    }

    private readonly object statusFileLock = new object(); // 全局锁，或用 per-device 锁

    private void UpdateStatus(string deviceId, string status, string error = "")
    {
        var obj = new
        {
            device_id = deviceId,
            status = status,
            error = error,
            timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        string path = Path.Combine(statusDir, $"status_{deviceId}.json");

        try
        {
            // 使用 lock 保证线程安全
            lock (statusFileLock)
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var sw = new StreamWriter(fs))
                {
                    string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    sw.Write(json);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[状态写入失败] {deviceId}：{ex.Message}");
        }
    }

    private void LogError(string deviceId, string content)
    {
        string path = Path.Combine(logDir, $"log_{deviceId}.txt");
        File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {content}\r\n");
    }
}