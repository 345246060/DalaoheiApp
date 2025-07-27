using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class Tooltip : Form
{
    private Label label;
    private Timer animationTimer;
    private int moveUpStep = 2;
    private int showDuration;
    private int elapsed = 0;
    private Color backgroundColor;

    [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
    private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect,
        int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

    public Tooltip(string message, bool isSuccess, int duration = 2000)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;

        this.showDuration = duration;
        backgroundColor = isSuccess ? Color.FromArgb(154, 205, 129) : Color.FromArgb(250, 128, 114); // 绿 / 红
        BackColor = backgroundColor;

        int maxTextWidth = 360;
        var font = new Font("Microsoft YaHei", 10);
        var padding = new Padding(10, 0, 10, 6);

        // 使用 TextRenderer 测量换行文本所需大小
        Size textSize = TextRenderer.MeasureText(
            message,
            font,
            new Size(maxTextWidth, 0),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

        int width = Math.Max(120, textSize.Width + padding.Left + padding.Right);
        int height = Math.Max(36, textSize.Height + padding.Top + padding.Bottom);

        Size = new Size(Math.Min(500, width), height);

        label = new Label
        {
            Text = message,
            Font = font,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = false,
            MaximumSize = new Size(maxTextWidth, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = padding,
            Dock = DockStyle.Fill
        };

        Controls.Add(label);

        // 圆角 5px
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 5, 5));

        animationTimer = new Timer();
        animationTimer.Interval = 50;
        animationTimer.Tick += Animate;
    }

    public void ShowAtCenter()
    {
        var screen = Screen.PrimaryScreen.WorkingArea;
        int x = screen.Left + (screen.Width - Width) / 2;
        int y = screen.Top + (screen.Height - Height) / 2; // 上下也居中

        Location = new Point(x, y);
        Opacity = 0;
        Show();
        animationTimer.Start();
    }

    private void Animate(object sender, EventArgs e)
    {
        if (Opacity < 1.0 && elapsed < 500)
        {
            Opacity += 0.1;
        }
        else
        {
            elapsed += animationTimer.Interval;
            if (elapsed >= showDuration)
            {
                Top -= moveUpStep;
                Opacity -= 0.05;
                if (Opacity <= 0)
                {
                    animationTimer.Stop();
                    Close();
                }
            }
        }
    }

    // ✅ 对外调用方法（含自定义显示时间）
    public static void ShowSuccess(string message, int duration = 500)
    {
        new Tooltip(message, true, duration).ShowAtCenter();
    }
    /// <summary>
    /// 错误提示
    /// </summary>
    /// <param name="message"></param>
    /// <param name="duration"></param>
    public static void ShowError(string message, int duration = 2000)
    {
        new Tooltip(message, false, duration).ShowAtCenter();
    }
}