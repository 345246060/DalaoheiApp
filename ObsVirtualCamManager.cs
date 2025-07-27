using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

public class ObsVirtualCamManager
{
    private readonly string obsExePath;
    private readonly string obsProfileName;
    private readonly string obsSceneCollectionName;
    private readonly string obsWorkingDir;
    private readonly int wsPort;
    private readonly string wsPassword;
    private WebSocket socket;
    private int requestCounter = 1;

    public ObsVirtualCamManager(string obsDir, string profile = "default", string sceneCollection = "default", int port = 4455, string password = "")
    {
        obsExePath = Path.Combine(obsDir, "bin", "64bit", "obs64.exe");
        obsWorkingDir = obsDir;
        obsProfileName = profile;
        obsSceneCollectionName = sceneCollection;
        wsPort = port;
        wsPassword = password;
    }

    public void StartObsInstance()
    {
        if (!File.Exists(obsExePath))
            throw new FileNotFoundException("OBS 启动文件未找到：" + obsExePath);

        var args = $"--multi --minimize-to-tray --profile \"{obsProfileName}\" --collection \"{obsSceneCollectionName}\"";
        var psi = new ProcessStartInfo
        {
            FileName = obsExePath,
            Arguments = args,
            WorkingDirectory = obsWorkingDir,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(psi);
    }

    public void Connect()
    {
        socket = new WebSocket($"ws://127.0.0.1:{wsPort}");
        socket.OnMessage += (s, e) => Console.WriteLine("[OBS] " + e.Data);
        socket.OnOpen += (s, e) => Console.WriteLine("[OBS] 连接成功");
        socket.OnError += (s, e) => Console.WriteLine("[OBS] 错误: " + e.Message);
        socket.Connect();
    }

    public void SendRequest(string requestType, object data = null)
    {
        var requestId = "req" + (requestCounter++);
        var payload = new
        {
            op = 6,
            d = new
            {
                requestType = requestType,
                requestId = requestId,
                requestData = data ?? new { }
            }
        };
        socket.Send(JsonConvert.SerializeObject(payload));
    }

    public void SwitchScene(string sceneName)
    {
        SendRequest("SetCurrentProgramScene", new { sceneName });
    }

    public void CreateScene(string sceneName)
    {
        SendRequest("CreateScene", new { sceneName });
    }

    public void RemoveScene(string sceneName)
    {
        SendRequest("RemoveScene", new { sceneName });
    }

    public void AddInputToScene(string sceneName, string sourceName, string inputKind, JObject settings)
    {
        SendRequest("CreateInput", new
        {
            sceneName,
            inputName = sourceName,
            inputKind,
            inputSettings = settings
        });
    }

    public void StartVirtualCam()
    {
        var startCam = new ProcessStartInfo
        {
            FileName = obsExePath,
            Arguments = "--startvirtualcam",
            WorkingDirectory = obsWorkingDir,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        Process.Start(startCam);
    }

    public static void StartFfmpegFromVirtualCam(string rtmpUrl, string videoDevice = "OBS Virtual Camera", string audioDevice = null)
    {
        string audioArg = string.IsNullOrEmpty(audioDevice) ? "" : $" -f dshow -i audio=\"{audioDevice}\" ";

        var psi = new ProcessStartInfo
        {
            FileName = "ffmpeg.exe",
            Arguments = $"-f dshow -i video=\"{videoDevice}\" {audioArg} -vcodec libx264 -preset veryfast -f flv {rtmpUrl}",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(psi);
    }

    public void Disconnect()
    {
        socket?.Close();
    }

    public static class InputSettingsTemplates
    {
        public static JObject FfmpegSource(string localFile) => new JObject
        {
            ["local_file"] = localFile,
            ["is_looping"] = true,
            ["is_hw_decoding"] = false
        };

        public static JObject ImageSource(string imagePath) => new JObject
        {
            ["file"] = imagePath
        };

        public static JObject TextGDI(string text) => new JObject
        {
            ["text"] = text,
            ["font"] = new JObject
            {
                ["face"] = "微软雅黑",
                ["size"] = 36
            },
            ["color1"] = 0xFFFFFFFF
        };

        public static JObject BrowserSource(string url, int width = 1280, int height = 720) => new JObject
        {
            ["url"] = url,
            ["width"] = width,
            ["height"] = height
        };

        public static JObject AudioInput(string deviceName) => new JObject
        {
            ["device_id"] = deviceName
        };

        public static JObject AudioOutput(string deviceName) => new JObject
        {
            ["device_id"] = deviceName
        };

        public static JObject WindowCapture(string windowName) => new JObject
        {
            ["window"] = windowName
        };

        public static JObject DisplayCapture() => new JObject();

        public static JObject GameCapture() => new JObject();
    }
}

/*
使用方式：
1. 启动 OBS：manager.StartObsInstance();
2. 连接 WebSocket：manager.Connect();
3. 添加场景和直播源：
   var settings = ObsVirtualCamManager.InputSettingsTemplates.FfmpegSource("test.mp4");
   manager.AddInputToScene("直播场景", "视频源", "ffmpeg_source", settings);
4. 启动虚拟摄像头：manager.StartVirtualCam();
5. 使用 FFmpeg 拉流推送：ObsVirtualCamManager.StartFfmpegFromVirtualCam("rtmp://xxx");
*/
