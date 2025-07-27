using CCWin;
using common;
using Flurl.Http;
using ImageMagick;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static EnhancedFfmpegManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using CosyVoiceClient;
using NAudio.Wave;

namespace DalaoheiApp
{
    public partial class MainForm : CCSkinMain
    {
        public static string FileVersion = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileVersion; 
        FfmpegTaskManager manager = new FfmpegTaskManager(); 
        public MainForm()
        {
            InitializeComponent(); 
        }
        private Dictionary<string, Verify_Form> douyinForms = new Dictionary<string, Verify_Form>();
        private readonly ConcurrentQueue<string> userQueue = new ConcurrentQueue<string>();
        private readonly HashSet<string> userSet = new HashSet<string>();
        private AudioStreamServer audioServer;
        private async void MainForm_Load(object sender, EventArgs es)
        {
            var writer = new RichTextBoxWriter(richTextBox1);
            Console.SetOut(writer);

            await LoadWebPToPictureBox(pictureBox1, GlobalData.short_img);
            //label1.Text = GlobalData.nickname;
            //label2.Text = GlobalData.short_id;
            Text = $"{GlobalData.sys_name}-内部测试交流学习，请遵守直播平台规则，不要用于违法活动和传播，测试后请删除，否则后果自负"; 
            string Cache_user = GlobalData.deviceId + "_" + GlobalData.user_code;
            string _userDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", Cache_user);
            Directory.CreateDirectory(_userDataFolder); 
        }
        /// <summary>
        /// 只保留中文、英文、数字字符，其余全部删除。
        /// </summary>
        public static string FilterChineseEnglishNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (IsChinese(c) || IsEnglishLetter(c) || IsDigit(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static bool IsChinese(char c)
        {
            return c >= 0x4e00 && c <= 0x9fff;
        }

        private static bool IsEnglishLetter(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        } 

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            var deviceId = GlobalData.deviceId;
            string cookie_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", deviceId);
            string cookies = CookieStorage.ReadCookie(cookie_path + "cookie.txt");
            var stream_id = GlobalData.stream_id;
            var room_id = GlobalData.room_id;
            var iid = GlobalData.iid; 
            if (deviceId != null && stream_id != null && room_id != null && iid != null)
            {
                await TaskManager.StopTaskAsync(deviceId); // 停止定时任务
                CloseDouyinFormById(deviceId); // 关闭窗口
                // 移除任务
                manager.RemoveTask(deviceId, out var msg2);
                Console.WriteLine(msg2); 
            }
            else
            {
                utility.logs("参数不够，或者没有开播！");
            }
            Application.Exit();
        }
        /// <summary>
        /// 登录抖音号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dSkinButton56_Click(object sender, EventArgs e)
        { 
            if(MessageBox.Show("内部测试交流学习，请遵守直播平台规则，不要用于违法活动和传播，测试后请删除，否则后果自负","系统提示",MessageBoxButtons.OKCancel,MessageBoxIcon.Question) == DialogResult.OK)
            {
                Verify_Form frm = new Verify_Form();
                frm.OnResultReturned += async (result) =>
                {
                    string finalUrl = GlobalData.short_img.Replace(@"\u0026", "&");
                    await LoadWebPToPictureBox(pictureBox1, finalUrl);
                    label1.Text = GlobalData.nickname;
                    label2.Text = GlobalData.short_id;
                    dSkinButton42.Enabled = true;
                    string localPath = DownloadImageToLocal(finalUrl);
                    GlobalData.localPath = localPath;
                };
                frm.Show();
            } 
        }
        /// <summary>
        /// 下载网络图片到本地并返回本地路径
        /// </summary>
        public static string DownloadImageToLocal(string imageUrl, string saveDir = null)
        {
            try
            {
                // 如果没有传 saveDir，就用程序当前目录
                if (string.IsNullOrEmpty(saveDir))
                    saveDir = AppDomain.CurrentDomain.BaseDirectory;

                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                // 文件名用 URL 最后的部分（带时间戳可防重名）
                string fileName = $"avatar_{GlobalData.deviceId}.jpg";
                string savePath = Path.Combine(saveDir, fileName);

                using (var client = new WebClient())
                {
                    // 设置请求头，伪装成浏览器，避免403
                    client.Headers.Add("User-Agent", "Mozilla/5.0");

                    client.DownloadFile(imageUrl, savePath);

                    //Console.WriteLine("[下载成功] 本地文件路径: " + savePath);
                    return savePath;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine("[错误] 下载图片失败: " + ex.Message);
                return null;
            }
        }
        /// <summary>
        /// 确定手机开播，获取推流码
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void dSkinButton42_Click(object sender, EventArgs e)
        { 
            //GlobalData.deviceId = utility.GenerateId(); 
            //GlobalData.iid = utility.GenerateId();
            string iid = GlobalData.iid;
            string short_id = GlobalData.short_id; 
            string cookie_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", GlobalData.user_code);
            string cookies = CookieStorage.ReadCookie(cookie_path + "cookie.txt");
            zhibopar ret = await douyin_api.GetLatestRoomInfoAsync(cookies, GlobalData.user_code, iid, short_id); // 获取直播间的数据
            if (ret != null)
            {
                GlobalData.stream_id = ret.stream_id;
                GlobalData.room_id = ret.room_id;
                label7.Text = ret.start_time;
                GlobalData.rtmp_push_url = ret.rtmp_push_url; 
                Tooltip.ShowSuccess($"{ret.nickname} - 直播间开播成功，请操作第三步完成卡黑！");
                dSkinButton50.Enabled = true;

                GlobalData.hardware_type = "CPU";
                //int randomNumber = 10000 + (Math.Abs(GlobalData.deviceId.GetHashCode()) % 1000);
                //audioServer = new AudioStreamServer(randomNumber);
                //audioServer.Start();
            }
            else
            {
                Tooltip.ShowError("未获取到直播间数据，请先开播。", 3000);
                return;
            }
        }
        private DanmuGenerator generator;  
        private CancellationTokenSource simulationTokenSource;

        /// <summary>
        /// 开始直播
        /// </summary>
        private async void dSkinButton50_Click(object sender, EventArgs e)
        {
            await Task.Delay(1);
            string deviceId = GlobalData.deviceId;
            string taskId = deviceId;
            string stream_id = GlobalData.stream_id;
            string room_id = GlobalData.room_id;
            string iid = GlobalData.iid;
            string cookie_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", GlobalData.user_code);
            string cookies = CookieStorage.ReadCookie(cookie_path + "cookie.txt");

            // 消息
            generator = new DanmuGenerator(30);
            generator.Start();

            // 启动“主播在线”心跳任务
            TaskManager.StartQuartzTask(taskId, cookies, stream_id, room_id, "2", deviceId, iid, 120); 
            timer3.Enabled = true;
            // 启动推流任务
            manager.AddTask(deviceId, GlobalData.rtmp_push_url, out var msg1);
            utility.logs(msg1);

            //// 启动模拟“主播离开”任务
            _ = Task.Run(async () =>
            {
                while (true)
                {  
                    int onlineDuration = new Random().Next(20, 35); // 正常播20~35分钟
                    utility.logs($"【行为模拟】将在 {onlineDuration} 分钟后模拟真人行为");
                    await Task.Delay(TimeSpan.FromMinutes(onlineDuration)); 
                    // ========== 主播离开 ==========
                    utility.logs("【行为模拟】开始模拟真人行为...");
                    await TaskManager.StopTaskAsync(taskId); // 停止原心跳
                    await Task.Delay(2 * 1000); 
                    TaskManager.StartQuartzTask(taskId, cookies, stream_id, room_id, "3", deviceId, iid, 5); // 离开状态

                    int awayMinutes = new Random().Next(50, 90); // 离开40~70秒
                    utility.logs($"【行为模拟】模拟真人行为 持续 {awayMinutes} 秒");
                    await Task.Delay(awayMinutes * 1000); 
                    // ========== 主播回归 ==========
                    utility.logs("【行为模拟】模拟真人行为结束");
                    await TaskManager.StopTaskAsync(taskId); // 停止“离开”心跳
                    TaskManager.StartQuartzTask(taskId, cookies, stream_id, room_id, "2", deviceId, iid, 120); // 在线状态   
                }
            });
        }
        /// <summary>
        /// 关闭窗口
        /// </summary>
        /// <param name="formId"></param>
        public void CloseDouyinFormById(string formId)
        {
            if (douyinForms.ContainsKey(formId))
            {
                var form = douyinForms[formId];
                if (!form.IsDisposed)
                {
                    form.Close();
                }
                douyinForms.Remove(formId);
            }
        }
        /// <summary>
        /// 关闭直播
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void skinButton4_Click(object sender, EventArgs e)
        { 
            var deviceId = GlobalData.deviceId;
            string cookie_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", GlobalData.user_code);
            string cookies = CookieStorage.ReadCookie(cookie_path + "cookie.txt");
            var stream_id = GlobalData.stream_id;
            var room_id = GlobalData.room_id;
            var iid = GlobalData.iid;
            var nickname = GlobalData.nickname;
            if (deviceId != null && stream_id != null && room_id != null && iid != null)
            {
                await TaskManager.StopTaskAsync(deviceId); // 停止定时任务
                CloseDouyinFormById(deviceId); // 关闭窗口
                // 移除任务
                manager.RemoveTask(deviceId, out var msg2);
                Console.WriteLine(msg2); 
                var ret = await douyin_api.ping_anchor(cookies, stream_id, room_id, "4", deviceId, iid);
                if (ret.code == 0)
                {
                    timer3.Enabled = false;
                    Tooltip.ShowSuccess("关播成功！");
                    label7.Text = string.Empty;
                    label9.Text = string.Empty;
                    generator?.Stop();
                }
                else
                {
                    Tooltip.ShowError(ret.msg, 3000);
                }
            }
            else
            {
                utility.logs("参数不够，或者没有开播！");
            }
            dSkinButton42.Enabled = false;
            dSkinButton50.Enabled = false;
        }
        public async Task LoadWebPToPictureBox(PictureBox picBox, string webpUrl)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] imageBytes = await client.GetByteArrayAsync(webpUrl);
                    using (MemoryStream memStream = new MemoryStream(imageBytes))
                    using (MagickImage image = new MagickImage(memStream))
                    {
                        // 转换为 Bitmap：先转为 byte[]，再创建 Bitmap
                        using (MemoryStream ms = new MemoryStream())
                        {
                            image.Format = MagickFormat.Bmp;
                            image.Write(ms);
                            ms.Position = 0;
                            picBox.Image = new Bitmap(ms);
                            picBox.SizeMode = PictureBoxSizeMode.Zoom;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                picBox.Image = Properties.Resources.dd_copy;
            }
        } 

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(label7.Text))
            {
                DateTime.TryParse(label7.Text.Trim(), out DateTime timestamp);
                TimeSpan remainingTime = DateTime.Now - timestamp;
                if (remainingTime.TotalSeconds > 0)
                {
                    // 计算总小时数（整数部分）
                    int totalHours = (int)remainingTime.TotalHours;
                    string formattedTime = $"{totalHours:D2}:{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}";
                    label9.Text = formattedTime;
                }
            } 
        }

        private async void timer3_Tick(object sender, EventArgs e)
        {
            try
            {
                string status = manager.GetStatus(GlobalData.deviceId);
                Console.WriteLine(status);
                if (status != null)
                {
                    if (status.Contains("停止"))
                    {
                        timer3.Enabled = false;
                    }
                } 
                while (DeviceQueueManager.Instance.TryDequeueDevice(out string deviceId))
                {
                    await TaskManager.StopTaskAsync(deviceId); // 停止定时任务
                    CloseDouyinFormById(deviceId); // 关闭窗口  
                    label7.Text = string.Empty;
                    label9.Text = string.Empty; 
                    dSkinButton42.Enabled = false;
                    dSkinButton50.Enabled = false;
                    timer3.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                dSkinButton42.Enabled = false;
                dSkinButton50.Enabled = false;
                Console.WriteLine("处理队列出错: ");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //       string filter =
            //"[0:v]split=3[base][gray][rot];" +
            //"[gray]format=gray[grayout];" +
            //"[base][grayout]blend=all_expr='if(lt(mod(T\\,10)\\,1)\\,B\\,A)'[grayblend];" +
            //"[rot]rotate=0.02*sin(t*2)[rotated];" +
            //"[grayblend][rotated]blend=all_expr='if(lt(mod(T\\,4)\\,2)\\,B\\,A)'," +
            //"drawbox=c=black@1:t=fill," +
            //"noise=alls=20:allf=t," +
            //"boxblur=2," +
            //"eq=contrast='if(lt(mod(t\\,5)\\,2)\\,1.05\\,0.95)'," +
            //"eq=brightness='if(lt(mod(t\\,8)\\,3)\\,-0.03\\,0.01)'," +
            //"hue='H=2*PI*t':s=0.8," +
            //"drawgrid=width=100:height=100:thickness=2:color=white@0.1," +
            //"chromashift=cbh=2:crh=-2:cbv=2:crv=-2," +
            //"hflip=enable='lt(mod(t\\,6)\\,3)'," +
            //"fade=in:st=0:d=1:alpha=1[bg];" +
            //"[bg][1:v]overlay=x='10+20*sin(t*3)':y='20+10*cos(t*2)':shortest=1[vout];" +
            //"[2:a][3:a]concat=n=2:v=0:a=1,aloop=loop=-1:size=1323000[beep];" +
            //"[beep][4:a]amix=inputs=2:duration=longest:dropout_transition=2,volume=0.005[aout]";

            //       string ffmpegCmd =
            //           "-y -re " +
            //           "-f lavfi -i color=c=black:s=720x1280:r=15 " +
            //           "-framerate 15 -loop 1 -i disturb_images/disturb_%03d.png " +
            //           "-f lavfi -i sine=frequency=800:duration=0.5 " +
            //           "-f lavfi -i aevalsrc=0:d=29.5 " +
            //           "-f lavfi -i anoisesrc=c=pink:r=44100 " +
            //           "-filter_complex \"" + filter + "\" " +
            //           "-map \"[vout]\" -map \"[aout]\" " +
            //           "-c:v libx264 -preset veryfast -b:v 50k -maxrate 50k -bufsize 50k -g 30 -pix_fmt yuv420p " +
            //           "-c:a aac -b:a 96k -ar 22050 -ac 1 " +
            //           "-t 60 test_output_ai_defense.mp4";

            //string filter =
            // "[0:v]split=3[base][gray][rot];" +
            // "[gray]format=gray[grayout];" +
            // "[base][grayout]blend=all_expr='if(lt(mod(T\\,10)\\,1)\\,B\\,A)'[grayblend];" +
            // "[rot]rotate=0.02*sin(t*2)[rotated];" +
            // "[grayblend][rotated]blend=all_expr='if(lt(mod(T\\,4)\\,2)\\,B\\,A)'," +
            // "drawbox=c=black@1:t=fill," +
            // "noise=alls=20:allf=t," +
            // "boxblur=2," +
            // "eq=contrast='if(lt(mod(t\\,5)\\,2)\\,1.05\\,0.95)'," +
            // "eq=brightness='if(lt(mod(t\\,8)\\,3)\\,-0.03\\,0.01)'," +
            // "hue='H=2*PI*t':s=0.8," +
            // "drawgrid=width=100:height=100:thickness=2:color=white@0.1," +
            // "chromashift=cbh=2:crh=-2:cbv=2:crv=-2," +
            // "hflip=enable='lt(mod(t\\,6)\\,3)'," +
            // "fade=in:st=0:d=1:alpha=1[vout];" +
            // "[1:a][2:a]concat=n=2:v=0:a=1,aloop=loop=-1:size=1323000[beep];" +
            // "[beep][3:a]amix=inputs=2:duration=longest:dropout_transition=2,volume=0.005[aout]";

            //string ffmpegCmd =
            //    "-y -re " +
            //    "-f lavfi -i color=c=black:s=720x1280:r=15 " +
            //    "-f lavfi -i sine=frequency=800:duration=0.5 " +
            //    "-f lavfi -i aevalsrc=0:d=29.5 " +
            //    "-f lavfi -i anoisesrc=c=pink:r=44100 " +
            //    "-filter_complex \"" + filter + "\" " +
            //    "-map \"[vout]\" -map \"[aout]\" " +
            //    "-c:v libx264 -preset veryfast -tune zerolatency -b:v 10k -maxrate 10k -bufsize 10k -g 30 -pix_fmt yuv420p " +
            //    "-c:a aac -b:a 96k -ar 22050 -ac 1 " +
            //    "-f flv " +
            //    "-user_agent \"DJI Fly/1109 CFNetwork/1568.200.51 Darwin/24.1.0\" " +
            //    "-rtmp_live live \"" + rtmpUrl + "\"";

            //Process process = null;
            //string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            //ProcessStartInfo psi = new ProcessStartInfo
            //{
            //    FileName = ffmpegPath,
            //    Arguments = ffmpegCmd,
            //    RedirectStandardError = true,
            //    UseShellExecute = false,
            //    CreateNoWindow = true
            //};

            //process = new Process { StartInfo = psi };
            //process.Start();

            //// 读取 FFmpeg 输出日志
            //string errorLog = process.StandardError.ReadToEnd();
            //process.WaitForExit();  // 等待 FFmpeg 执行完毕

            //// 输出错误日志（你也可以写到文件中）
            //Console.WriteLine(errorLog);

            //// 检查输出文件是否存在
            //string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_output.mp4");
            //if (File.Exists(outputPath))
            //{
            //    Console.WriteLine("✅ 文件生成成功：" + outputPath);
            //}
            //else
            //{
            //    Console.WriteLine("❌ 文件未生成，请查看 FFmpeg 错误日志！");
            //}
        }

        private static readonly string danmuFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "txt", "danmu_pool.txt");

        public string GetRandomDanmu()
        {
            try
            {
                if (!File.Exists(danmuFilePath))
                    return "的到来。";

                var lines = File.ReadAllLines(danmuFilePath)
                                .Select(line => line.Trim())
                                .Where(line => !string.IsNullOrEmpty(line))
                                .ToList();

                if (lines.Count == 0)
                    return "来到我直播间";

                var rand = new Random();
                return lines[rand.Next(lines.Count)];
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }
        private async Task HandleTtsBroadcastAsync()
        {
            var tempList = new List<string>();
            string item;

            while (userQueue.TryDequeue(out item))
            {
                userSet.Remove(item);
                if (!tempList.Contains(item))
                    tempList.Add(item);
            }

            // 如果超过3个，只保留3个随机的
            if (tempList.Count > 3)
            {
                var rand = new Random();
                tempList = tempList.OrderBy(_ => rand.Next()).Take(3).ToList();
            }

            foreach (var name in tempList)
            {
                try
                {
                    // 生成 WAV 语音文件
                    string wavPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tts.wav");
                    using (var synth = new System.Speech.Synthesis.SpeechSynthesizer())
                    {
                        synth.SetOutputToWaveFile(wavPath);
                        synth.Speak($"欢迎{name} !! {GetRandomDanmu()}");
                    } 
                    // 转 MP3
                    string mp3Path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tts_result.mp3");
                    string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
                    if (!File.Exists(ffmpegPath))
                    {
                        MessageBox.Show("缺少 ffmpeg.exe");
                        return;
                    }  
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ffmpegPath,
                            Arguments = $"-y -i \"{wavPath}\" -ar 22050 -ac 1 -b:a 64k \"{mp3Path}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        }
                    };
                    proc.Start();
                    proc.WaitForExit(); 
                    byte[] mp3Bytes = File.ReadAllBytes(mp3Path);
                    int randomNumber = 10000 + (Math.Abs(GlobalData.deviceId.GetHashCode()) % 1000);
                    audioServer.EnqueueMp3($"device{randomNumber}", mp3Bytes);
                    Console.WriteLine($"欢迎 {name} 的语音已生成并推送。");

                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("TTS 失败：" + ex.Message);
                }
            }
        }
        private async void timer4_Tick(object sender, EventArgs e)
        {
            timer4.Enabled = false;

            Task.Run(async () =>
            {
                try
                {
                    await HandleTtsBroadcastAsync();
                }
                catch (Exception ex)
                { 
                    Console.WriteLine("TTS 异常：" + ex.Message);
                }
                finally
                { 
                    this.Invoke((Action)(() => timer4.Enabled = true));
                }
            });
        }

       
    }
}
