using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace common
{
    public class RichTextBoxWriter : TextWriter
    {
        private readonly RichTextBox _richTextBox;
        private readonly string _logDir;
        private readonly string _baseFileName = "app";
        private readonly int _maxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        private readonly int _maxBackupFiles = 5;

        public RichTextBoxWriter(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox;
            _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(_logDir);
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string msg = $"{timestamp} - {value}";

            if (msg.Contains("浏览器控制台消息"))
                return;

            // 写 UI（防卡顿仍可用缓冲队列法进一步优化）
            if (_richTextBox.InvokeRequired)
                _richTextBox.BeginInvoke(new Action(() => AddItem(msg)));
            else
                AddItem(msg);

            // 写文件
            WriteToFile(msg);
        }

        private void AddItem(string msg)
        {
            _richTextBox.AppendText(msg);
            if (msg.Contains("错误"))
            {
                SetSelectionColor(Color.Red, msg);
            }
            else if (msg.Contains("结束"))
            {
                SetSelectionColor(Color.SeaGreen, msg);
            }
            else if (msg.Contains("分割标志"))
            {
                SetSelectionColor(Color.Maroon, msg);
            }
            else if (msg.Contains("警告"))
            {
                SetSelectionColor(Color.Orange, msg);
            }

            _richTextBox.AppendText(Environment.NewLine);
            _richTextBox.ScrollToCaret();
        }

        private void SetSelectionColor(Color color, string msg)
        {
            try
            {
                _richTextBox.SelectionStart = _richTextBox.Text.Length - msg.Length;
                _richTextBox.SelectionLength = msg.Length;
                _richTextBox.SelectionColor = color;
            }
            catch { }
        }

        private void WriteToFile(string msg)
        {
            try
            {
                string currentLogPath = Path.Combine(_logDir, _baseFileName + ".log");
                RotateLogIfNeeded(currentLogPath);

                File.AppendAllText(currentLogPath, msg + Environment.NewLine);
            }
            catch
            {
                // 忽略日志写入异常，避免主流程崩溃
            }
        }

        private void RotateLogIfNeeded(string currentLogPath)
        {
            try
            {
                if (File.Exists(currentLogPath))
                {
                    FileInfo fi = new FileInfo(currentLogPath);
                    if (fi.Length >= _maxFileSizeBytes)
                    {
                        // 删除最旧
                        string oldest = Path.Combine(_logDir, $"{_baseFileName}.{_maxBackupFiles}.log");
                        if (File.Exists(oldest))
                            File.Delete(oldest);

                        // 向后移动
                        for (int i = _maxBackupFiles - 1; i >= 1; i--)
                        {
                            string src = Path.Combine(_logDir, $"{_baseFileName}.{i}.log");
                            string dest = Path.Combine(_logDir, $"{_baseFileName}.{i + 1}.log");
                            if (File.Exists(src))
                                File.Move(src, dest);
                        }

                        // 当前日志转为 .1
                        string firstBackup = Path.Combine(_logDir, $"{_baseFileName}.1.log");
                        File.Move(currentLogPath, firstBackup);
                    }
                }
            }
            catch
            {
                // 忽略轮转失败
            }
        }
    }
}