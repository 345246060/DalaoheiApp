using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DalaoheiApp
{
    public partial class LoadingForm : Form
    {
        public LoadingForm()
        {
            InitializeComponent();
            // 设置窗口属性
            this.FormBorderStyle = FormBorderStyle.None; // 无边框
            this.StartPosition = FormStartPosition.Manual; // 手动设置位置
            this.TopMost = true; // 保持在最上层
            this.BackColor = Color.Gray;
            this.Opacity = 0.8; // 半透明效果
            this.ShowInTaskbar = false; // 不在任务栏显示

            // 添加一个简单的Label作为提示
            Label loadingLabel = new Label();
            loadingLabel.Text = "加载中，请稍候...";
            loadingLabel.AutoSize = true;
            loadingLabel.ForeColor = Color.White;
            loadingLabel.Font = new Font("Microsoft YaHei", 12);
            loadingLabel.Location = new Point((this.Width - loadingLabel.Width) / 2, (this.Height - loadingLabel.Height) / 2);
            this.Controls.Add(loadingLabel);
        }

        private void LoadingForm_Load(object sender, EventArgs e)
        {

        }
    }
}
