using common;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

public static class TaskManager
{
    private static readonly IScheduler scheduler;
    private static readonly ConcurrentDictionary<string, IJobDetail> jobs = new ConcurrentDictionary<string, IJobDetail>();

    static TaskManager()
    {
        scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
        scheduler.Start().Wait();
    }

    public static void StartQuartzTask(string taskId, string cookies, string stream_id, string room_id, string status, string device_id, string iid,int seconds)
    {
        var job = JobBuilder.Create<PingAnchorJob>()
            .WithIdentity(taskId)
            .UsingJobData("cookies", cookies)
            .UsingJobData("stream_id", stream_id)
            .UsingJobData("room_id", room_id)
            .UsingJobData("status", status)
            .UsingJobData("device_id", device_id)
            .UsingJobData("iid", iid)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger_{taskId}")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(seconds).RepeatForever())
            .Build();

        scheduler.ScheduleJob(job, trigger).Wait();
        jobs[taskId] = job;
    }

    public static async Task StopTaskAsync(string taskId)
    {
        if (jobs.TryRemove(taskId, out var job))
        {
            await scheduler.DeleteJob(job.Key);
            utility.logs("已停止任务：{taskId}"); 
        }
    }

    public static async Task StopAllAsync()
    {
        foreach (var kv in jobs)
        {
            await scheduler.DeleteJob(kv.Value.Key);
        }
        jobs.Clear();
        utility.logs("已停止所有任务");
    }
}

public class PingAnchorJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string cookies = context.MergedJobDataMap.GetString("cookies");
        string stream_id = context.MergedJobDataMap.GetString("stream_id");
        string room_id = context.MergedJobDataMap.GetString("room_id");
        string status = context.MergedJobDataMap.GetString("status");
        string device_id = context.MergedJobDataMap.GetString("device_id");
        string iid = context.MergedJobDataMap.GetString("iid");

        try
        {
            var result = await douyin_api.ping_anchor(cookies, stream_id, room_id, status, device_id, iid);
            utility.logs($"[{DateTime.Now:HH:mm:ss}] 设备 {device_id} ping: {result?.msg}");
        }
        catch (Exception ex)
        {
            utility.logs($"[{DateTime.Now:HH:mm:ss}] ping_anchor 执行失败: {ex.Message}");
        }
    }
} 