using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public class AudioWebSocketServer
{
    private readonly HttpListener listener = new HttpListener();
    private readonly ConcurrentDictionary<string, WebSocket> clientMap = new ConcurrentDictionary<string, WebSocket>();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<byte[]>> audioQueueMap = new ConcurrentDictionary<string, ConcurrentQueue<byte[]>>();
    private readonly int port;

    public AudioWebSocketServer(int port)
    {
        this.port = port;
        listener.Prefixes.Add($"http://+:{port}/audio/");
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("[WebSocket 音频服务] 启动成功");
        Task.Run(() => AcceptLoop());
    }

    private async Task AcceptLoop()
    {
        while (true)
        {
            var context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                string deviceId = context.Request.RawUrl?.Replace("/audio/", "").Trim('/');
                if (string.IsNullOrEmpty(deviceId))
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                    continue;
                }

                var wsContext = await context.AcceptWebSocketAsync(null);
                var socket = wsContext.WebSocket;
                clientMap[deviceId] = socket;
                audioQueueMap.TryAdd(deviceId, new ConcurrentQueue<byte[]>());

                Console.WriteLine($"[连接] {deviceId} 已接入");
                _ = Task.Run(() => HandleClient(deviceId, socket));
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
    }

    private async Task HandleClient(string deviceId, WebSocket socket)
    {
        var queue = audioQueueMap[deviceId];

        try
        {
            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open)
            {
                if (!queue.TryDequeue(out var audioData))
                {
                    await Task.Delay(10);
                    continue;
                }

                var segment = new ArraySegment<byte>(audioData);
                await socket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None);
                await Task.Delay(20); // 控制推送频率
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[错误] 客户端 {deviceId} 异常: {ex.Message}");
        }
        finally
        {
            if (clientMap.TryRemove(deviceId, out _))
                Console.WriteLine($"[断开] {deviceId} 已断开");
        }
    }

    public void EnqueuePcm(string deviceId, byte[] pcmData)
    {
        if (!audioQueueMap.ContainsKey(deviceId))
            audioQueueMap[deviceId] = new ConcurrentQueue<byte[]>();

        var queue = audioQueueMap[deviceId];

        // 分块推送（PCM）
        const int chunkSize = 2048;
        for (int i = 0; i < pcmData.Length; i += chunkSize)
        {
            int len = Math.Min(chunkSize, pcmData.Length - i);
            byte[] chunk = new byte[len];
            Array.Copy(pcmData, i, chunk, 0, len);
            queue.Enqueue(chunk);
        }
    }
}
