using CCWin;
using common;
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
    public partial class Form1 : CCSkinMain
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text.Trim()))
            {
                MessageBox.Show("请填写18位幸运数字");
                return;
            }
            if (string.IsNullOrWhiteSpace(textBox2.Text.Trim()))
            {
                MessageBox.Show("请填写18位随缘数字");
                return;
            }
            GlobalData.user_code = textBox1.Text.Trim() + "" + textBox1.Text.Trim();
            GlobalData.deviceId = textBox1.Text.Trim();
            GlobalData.iid = textBox2.Text.Trim();
            this.Hide();
            MainForm frm = new MainForm();
            frm.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = utility.GenerateId();
        }

        private void button4_Click(object sender, EventArgs e)
        {

            textBox2.Text = utility.GenerateId();
        }
    }
}
