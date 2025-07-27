using common;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class FfmpegQuartzManager
{
    private readonly string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
    private readonly ConcurrentDictionary<string, Process> processMap = new ConcurrentDictionary<string, Process>();
    private readonly ConcurrentDictionary<string, string> urlMap = new ConcurrentDictionary<string, string>();
    private readonly IScheduler scheduler;

    public FfmpegQuartzManager()
    {
        scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
        scheduler.Start().Wait();
    }
    // 关键方法，注册到Scheduler.Context（初始化后调用一次即可）
    public void RegisterToSchedulerContext()
    {
        // 这样Job里可以通过 scheduler.Context["ffmpegManager"] 访问到全局实例
        if (!scheduler.Context.ContainsKey("ffmpegManager"))
            scheduler.Context.Put("ffmpegManager", this);
    }
    // 启动推流+守护
    public bool AddTask(string deviceId, string rtmpUrl, out string message)
    {
        if (processMap.ContainsKey(deviceId))
        {
            message = $"设备 {deviceId} 推流任务已存在。";
            return false;
        }

        try
        {
            // 启动一次ffmpeg
            var process = StartFfmpegProcess(deviceId, rtmpUrl);
            processMap[deviceId] = process;
            urlMap[deviceId] = rtmpUrl;

            // 启动Quartz守护Job
            var job = JobBuilder.Create<FfmpegWatchDogJob>()
                .WithIdentity(deviceId, "ffmpeg")
                .UsingJobData("deviceId", deviceId)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(deviceId, "ffmpeg")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(15).RepeatForever())
                .Build();

            scheduler.ScheduleJob(job, trigger).Wait();

            message = $"推流任务 {deviceId} 启动成功。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"推流任务 {deviceId} 启动失败：{ex.Message}";
            return false;
        }
    }

    // 关闭任务
    public bool StopTask(string deviceId, out string message)
    {
        try
        {
            scheduler.DeleteJob(new JobKey(deviceId, "ffmpeg")).Wait();

            if (processMap.TryRemove(deviceId, out var process))
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                    process.Dispose();
                }
            }
            urlMap.TryRemove(deviceId, out _);

            message = $"推流任务 {deviceId} 已关闭。";
            return true;
        }
        catch (Exception ex)
        {
            message = $"停止任务失败：{ex.Message}";
            return false;
        }
    }

    // 启动FFmpeg进程
    private Process StartFfmpegProcess(string deviceId, string rtmpUrl)
    {
        string filter = "[0:v]drawbox=c=black@1:t=fill," +
                                     "noise=alls=20:allf=t," +
                                     "boxblur=2," +
                                     "eq=contrast='if(lt(mod(t\\,5)\\,2)\\,1.03\\,1)'," +
                                     "eq=brightness='if(lt(mod(t\\,8)\\,3)\\,-0.02\\,0)'";
        for (int i = 1; i <= 20; i++)
        {
            string y = $"h*{(0.05 + (0.9 - 0.05) / 19 * (i - 1)):0.###}";
            int speed = 60 + (i % 5) * 10;
            filter +=
                $",drawtext=fontfile=/Windows/Fonts/simhei.ttf:" +
                $"textfile='danmu{i}.read.txt':reload=1:fontcolor=white@0.5:fontsize=30:" +
                $"x='w - mod(t*{speed}\\, w+text_w)':y='{y}'";
        }
        filter = filter.TrimEnd(',') + "[vout];" +
                 "[1:a][2:a]concat=n=2:v=0:a=1,aloop=loop=-1:size=1323000[beep];" +
                 "[beep][3:a]amix=inputs=2:duration=longest:dropout_transition=2,volume=0.0005[aout]";

        string ffmpegCmd =
            "-y -re " +
            "-f lavfi -i color=c=black:s=720x1280:r=15 " +
            "-f lavfi -i sine=frequency=800:duration=0.5 " +
            "-f lavfi -i aevalsrc=0:d=29.5 " +
            "-f lavfi -i anoisesrc=c=pink:r=44100 " +
            "-filter_complex \"" + filter + "\" " +
            "-map \"[vout]\" -map \"[aout]\" " +
            "-c:v libx264 -preset veryfast -b:v 10k -maxrate 10k -bufsize 10k -g 30 -pix_fmt yuv420p " +
            "-c:a aac -b:a 96k -ar 22050 -ac 1 " +
            "-f flv " +
            "-user_agent \"DJI Fly/1109 CFNetwork/1568.200.51 Darwin/24.1.0\" " +
            "-metadata title=\"直播间\" " +
            "-rtmp_live live \"" + rtmpUrl + "\"";

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = ffmpegCmd,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = new Process { StartInfo = psi };
        process.Start();
        // 日志可以补充写
        return process;
    }

    // 守护Job
    public class FfmpegWatchDogJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            string deviceId = context.JobDetail.JobDataMap.GetString("deviceId");

            try
            {
                var schedulerContext = context.Scheduler.Context;
                if (!schedulerContext.ContainsKey("ffmpegManager"))
                {
                    Console.WriteLine($"[{DateTime.Now}] 未找到ffmpegManager实例，deviceId={deviceId}");
                    return;
                }
                var manager = schedulerContext["ffmpegManager"] as FfmpegQuartzManager;
                if (manager == null)
                {
                    Console.WriteLine($"[{DateTime.Now}] ffmpegManager为空，deviceId={deviceId}");
                    return;
                }

                string cookie_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", deviceId);
                string cookies = CookieStorage.ReadCookie(cookie_path + "cookie.txt");
                // 1. 检查直播状态
                var result = await douyin_api.ping_anchor(cookies, GlobalData.stream_id, GlobalData.room_id, "2", deviceId, GlobalData.iid);
                if (result.code != 0 && result.msg != "正在直播") // 你需按实际字段判断
                {
                    Console.WriteLine($"[{DateTime.Now}] 检测到已关播 deviceId={deviceId}，将停止守护与推流进程。"); 
                    // 2. 停止 Quartz 守护任务
                    await context.Scheduler.DeleteJob(context.JobDetail.Key); 
                    // 3. 停止/清理 ffmpeg 进程
                    if (manager.processMap.TryRemove(deviceId, out var process))
                    {
                        try
                        {
                            if (process != null && !process.HasExited)
                            {
                                process.Kill();
                                process.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now}] 关闭进程失败: {ex}");
                        }
                    }
                    return;
                }

                // 4. 正常情况下守护推流进程
                if (!manager.processMap.TryGetValue(deviceId, out var ffmpegProc) || ffmpegProc == null || ffmpegProc.HasExited)
                {
                    Console.WriteLine($"[{DateTime.Now}] 检测到ffmpeg进程丢失/已退出，deviceId={deviceId}，尝试重启...");
                    try
                    { 
                        zhibopar ret = await douyin_api.GetLatestRoomInfoAsync(cookies, deviceId, GlobalData.iid, GlobalData.short_id); // 获取直播间的数据
                        if (ret != null)
                        {
                            GlobalData.stream_id = ret.stream_id;
                            GlobalData.room_id = ret.room_id; 
                            GlobalData.rtmp_push_url = ret.rtmp_push_url;  
                        }
                        var newProcess = manager.StartFfmpegProcess(deviceId, ret.rtmp_push_url);
                        manager.processMap[deviceId] = newProcess;
                        Console.WriteLine($"[{DateTime.Now}] 已重启ffmpeg进程，deviceId={deviceId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now}] 重启ffmpeg进程失败，deviceId={deviceId}，异常：{ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now}] WatchDogJob异常，deviceId={deviceId}，异常：{ex}");
            }
            await Task.CompletedTask;
        }
    }
}