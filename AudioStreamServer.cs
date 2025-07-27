using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using NAudio.Wave;

public class AudioStreamServer
{
    private readonly HttpListener listener = new HttpListener();
    private readonly ConcurrentDictionary<string, BlockingCollection<byte[]>> audioMap = new ConcurrentDictionary<string, BlockingCollection<byte[]>>();
    private readonly byte[] silentPcmFrame;
    private readonly int sampleRate = 22050;
    private readonly int channels = 1;

    public AudioStreamServer(int port)
    {
        listener.Prefixes.Add("http://+:" + port + "/audio/");
        silentPcmFrame = GenerateSilentPcmFrame();

        // 初始化设备锁
        deviceLocks = new ConcurrentDictionary<string, object>();
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("[服务] 启动成功");

        ThreadPool.QueueUserWorkItem(delegate (object state)
        {
            while (true)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(delegate (object ctx) { HandleClient((HttpListenerContext)ctx); }, context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[错误] 监听失败: " + ex.Message);
                }
            }
        });
    }

    public void EnqueueMp3(string deviceId, byte[] mp3Data)
    {
        // 获取设备锁
        object deviceLock;
        if (!deviceLocks.TryGetValue(deviceId, out deviceLock))
        {
            deviceLock = new object();
            deviceLocks.TryAdd(deviceId, deviceLock);
        }

        lock (deviceLock)
        {
            var queue = audioMap.GetOrAdd(deviceId, delegate { return new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>()); });

            byte[] pcmData = ConvertMp3ToPcm(mp3Data);

            // 拆分为小段注入，模拟实时播放
            foreach (var chunk in SplitIntoChunks(pcmData, 2048)) // 每块减小一半
            {
                queue.Add(chunk);
                Thread.Sleep(20); // 每秒约50帧
            }
        }
    }
    private readonly ConcurrentDictionary<string, object> deviceLocks = new ConcurrentDictionary<string, object>();
    private readonly ConcurrentDictionary<string, Process> ffmpegProcessMap = new ConcurrentDictionary<string, Process>();
    private readonly ConcurrentDictionary<string, Stream> responseStreamMap = new ConcurrentDictionary<string, Stream>();
    private void HandleClient(HttpListenerContext context)
    {
        string deviceId = context.Request.RawUrl?.Replace("/audio/", string.Empty).Trim('/');
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        Console.WriteLine($"[连接请求] 设备: {deviceId}");

        // 获取设备锁
        object deviceLock;
        if (!deviceLocks.TryGetValue(deviceId, out deviceLock))
        {
            deviceLock = new object();
            deviceLocks.TryAdd(deviceId, deviceLock);
        }

        lock (deviceLock)
        {
            // 清理旧的输出流
            Stream oldStream;
            if (responseStreamMap.TryRemove(deviceId, out oldStream))
            {
                try
                {
                    oldStream.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[关闭旧流失败] {ex.Message}");
                }
            }

            // 结束旧的 ffmpeg 进程
            Process oldProc;
            if (ffmpegProcessMap.TryRemove(deviceId, out oldProc))
            {
                try
                {
                    if (!oldProc.HasExited)
                    {
                        oldProc.Kill();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[关闭旧进程失败] {ex.Message}");
                }
            }

            // 确保队列存在
            var queue = audioMap.GetOrAdd(deviceId, _ => new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>()));
            audioMap[deviceId] = queue;

            // 异步启动新连接
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var response = context.Response;
                response.StatusCode = 200;
                response.ContentType = "audio/mpeg";

                responseStreamMap[deviceId] = response.OutputStream;

                Process ffmpeg = null;

                try
                {
                    ffmpeg = StartFfmpegProcess(response.OutputStream);
                    ffmpegProcessMap[deviceId] = ffmpeg;

                    var stdin = ffmpeg.StandardInput.BaseStream;

                    while (true)
                    {
                        byte[] pcm;
                        if (!queue.TryTake(out pcm, TimeSpan.FromMilliseconds(100)))
                        {
                            pcm = silentPcmFrame;
                        }

                        stdin.Write(pcm, 0, pcm.Length);
                        stdin.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[断开] 设备: {deviceId}，异常: {ex.Message}");
                }
                finally
                {
                    try { response.OutputStream.Close(); } catch { }

                    if (ffmpeg != null)
                    {
                        try { if (!ffmpeg.HasExited) ffmpeg.Kill(); } catch { }
                    }

                    // 显式释放
                    if (ffmpegProcessMap.TryRemove(deviceId, out Process _)) { }
                    if (responseStreamMap.TryRemove(deviceId, out Stream _)) { }
                }
            });
        }
    }

    private Process StartFfmpegProcess(Stream outputStream)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = "-f s16le -ar 22050 -ac 1 -i pipe:0 -f mp3 -compression_level 0 -frame_duration 20 -flush_packets 1 -fflags +nobuffer -",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process();
        process.StartInfo = psi;
        process.Start();

        ThreadPool.QueueUserWorkItem(delegate (object state)
        {
            try
            {
                var stdout = process.StandardOutput.BaseStream;
                byte[] buffer = new byte[4096];
                int read;
                while ((read = stdout.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outputStream.Write(buffer, 0, read);
                    outputStream.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[转发异常]：" + ex.Message);
            }
            finally
            {
                try { outputStream.Close(); } catch { }
            }
        });

        return process;
    }

    private byte[] GenerateSilentPcmFrame()
    {
        int durationMs = 100;
        int samples = sampleRate * durationMs / 1000;
        return new byte[samples * 2 * channels];
    }

    private byte[] ConvertMp3ToPcm(byte[] mp3Bytes)
    {
        using (var mp3Stream = new MemoryStream(mp3Bytes))
        using (var mp3Reader = new Mp3FileReader(mp3Stream))
        using (var resampler = new MediaFoundationResampler(mp3Reader, new WaveFormat(22050, 16, 1)))
        {
            byte[] buffer = new byte[mp3Reader.Length * 4];
            int read = resampler.Read(buffer, 0, buffer.Length);
            byte[] result = new byte[read];
            Array.Copy(buffer, result, read);
            return result;
        }
    }

    private static byte[][] SplitIntoChunks(byte[] data, int chunkSize)
    {
        int chunkCount = (data.Length + chunkSize - 1) / chunkSize;
        byte[][] result = new byte[chunkCount][];
        for (int i = 0; i < chunkCount; i++)
        {
            int len = Math.Min(chunkSize, data.Length - i * chunkSize);
            byte[] chunk = new byte[len];
            Array.Copy(data, i * chunkSize, chunk, 0, len);
            result[i] = chunk;
        }
        return result;
    }
}