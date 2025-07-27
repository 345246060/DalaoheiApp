// DanmuGenerator.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using common;

public class DanmuGenerator
{
    private readonly List<string> danmuPool = new List<string>();
    private const string ExtraChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private readonly string[] targetFiles = Enumerable.Range(1, 20).Select(i => $"danmu{i}.read.txt").ToArray();
    private readonly Random rand = new Random();
    private readonly int intervalSeconds;

    private CancellationTokenSource _cts;
    private Task _workerTask;
    private double noiseLevel = 0.6;

    public DanmuGenerator(int intervalSeconds = 5)
    {
        this.intervalSeconds = intervalSeconds;
        LoadDanmuPool();
        InitializeFiles();
    }

    private void LoadDanmuPool()
    {
        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "txt", "danmu_pool.txt");
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path, Encoding.UTF8).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                danmuPool.Clear();
                danmuPool.AddRange(lines);
                utility.logs($"成功读取 {danmuPool.Count} 条弹幕内容");
            }
            else
            {
                utility.logs("未找到 danmu_pool.txt 文件！");
            }
        }
        catch (Exception ex)
        {
            utility.logs("读取弹幕池失败：" + ex.Message);
        }
    }

    private void InitializeFiles()
    {
        foreach (var file in targetFiles)
        {
            try
            {
                if (!File.Exists(file) || new FileInfo(file).Length == 0)
                {
                    File.WriteAllText(file, " 😊", Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                utility.logs($"初始化 {file} 失败：" + ex.Message);
            }
        }
    }

    public void Start()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
            return;

        _cts = new CancellationTokenSource();

        _workerTask = Task.Run(() =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (danmuPool.Count == 0)
                    {
                        utility.logs("弹幕池为空，跳过本轮更新");
                    }
                    else
                    {
                        var selected = danmuPool.OrderBy(_ => rand.Next()).Take(targetFiles.Length).ToArray();

                        for (int i = 0; i < targetFiles.Length; i++)
                        {
                            string noisyText = AddNoise(selected[i]);
                            SafeReplaceFile(targetFiles[i], noisyText);
                            Task.Delay(10).Wait();
                        }
                    }
                }
                catch (Exception ex)
                {
                    utility.logs("弹幕写入失败：" + ex.Message);
                }

                try
                {
                    Task.Delay(intervalSeconds * 1000, _cts.Token).Wait();
                }
                catch (TaskCanceledException) { break; }
            }
        }, _cts.Token);
    }

    public void Stop()
    {
        if (_cts == null) return;

        _cts.Cancel();
        try { _workerTask?.Wait(); }
        catch { }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _workerTask = null;
            utility.logs("弹幕生成器已停止");
        }
    }

    private string AddNoise(string input)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            sb.Append(c);
            if (rand.NextDouble() < noiseLevel)
                sb.Append(ExtraChars[rand.Next(ExtraChars.Length)]);
        }

        string[] tails = { " 234", "~~", "...", "!!!", "！", "？", "～", "", " 23", " 23", " 23234" };
        sb.Append(tails[rand.Next(tails.Length)]);
        return sb.ToString();
    }

    private void SafeReplaceFile(string path, string content)
    {
        try
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                sw.Write(content);
            }
        }
        catch
        {
            // 忽略所有异常，下一轮定时任务会再次写入，不会影响主程序稳定
        }
    }

    private void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
