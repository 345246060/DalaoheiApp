using CCWin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using common;

namespace DalaoheiApp
{
    public partial class ExtractRtmpPushUrlForm : CCSkinMain
    {
        public ExtractRtmpPushUrlForm()
        {
            InitializeComponent();
        }

        private void ExtractRtmpPushUrlForm_Load(object sender, EventArgs e)
        {

        }

        private void dSkinButton42_Click(object sender, EventArgs e)
        {
            string str = textBox1.Text.Trim();
            string rtmp_push_url = utility.ExtractRtmpPushUrl(str);
            if (string.IsNullOrWhiteSpace(rtmp_push_url))
            {
                Tooltip.ShowError("没有解析到正确的推流码，请确认你提交的格式是否准确！", 3000);
                return;
            }
            else
            {
                GlobalData.rtmp_push_url = rtmp_push_url;
                Tooltip.ShowSuccess("推流码提取成功！");
                this.Close();
            }
        }

        private void dSkinButton54_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
