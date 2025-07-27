
namespace DalaoheiApp
{
    partial class ExtractRtmpPushUrlForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExtractRtmpPushUrlForm));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.dSkinButton42 = new DSkin.Controls.DSkinButton();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.dSkinButton54 = new DSkin.Controls.DSkinButton();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Location = new System.Drawing.Point(7, 31);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(775, 250);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "推流码粘贴";
            // 
            // dSkinButton42
            // 
            this.dSkinButton42.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.dSkinButton42.BaseColor = System.Drawing.Color.DodgerBlue;
            this.dSkinButton42.ButtonBorderWidth = 1;
            this.dSkinButton42.Cursor = System.Windows.Forms.Cursors.Hand;
            this.dSkinButton42.DialogResult = System.Windows.Forms.DialogResult.None;
            this.dSkinButton42.ForeColor = System.Drawing.Color.White;
            this.dSkinButton42.HoverColor = System.Drawing.Color.Empty;
            this.dSkinButton42.HoverImage = null;
            this.dSkinButton42.Location = new System.Drawing.Point(7, 287);
            this.dSkinButton42.Name = "dSkinButton42";
            this.dSkinButton42.NormalImage = null;
            this.dSkinButton42.PressColor = System.Drawing.Color.Empty;
            this.dSkinButton42.PressedImage = null;
            this.dSkinButton42.Radius = 10;
            this.dSkinButton42.ShowButtonBorder = true;
            this.dSkinButton42.Size = new System.Drawing.Size(112, 29);
            this.dSkinButton42.TabIndex = 38;
            this.dSkinButton42.Text = "解析提取";
            this.dSkinButton42.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.dSkinButton42.TextPadding = 0;
            this.dSkinButton42.Click += new System.EventHandler(this.dSkinButton42_Click);
            // 
            // textBox1
            // 
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox1.Location = new System.Drawing.Point(3, 17);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(769, 230);
            this.textBox1.TabIndex = 0;
            // 
            // dSkinButton54
            // 
            this.dSkinButton54.BaseColor = System.Drawing.Color.Tomato;
            this.dSkinButton54.ButtonBorderWidth = 1;
            this.dSkinButton54.DialogResult = System.Windows.Forms.DialogResult.None;
            this.dSkinButton54.ForeColor = System.Drawing.Color.White;
            this.dSkinButton54.HoverColor = System.Drawing.Color.Empty;
            this.dSkinButton54.HoverImage = null;
            this.dSkinButton54.Location = new System.Drawing.Point(222, 287);
            this.dSkinButton54.Name = "dSkinButton54";
            this.dSkinButton54.NormalImage = null;
            this.dSkinButton54.PressColor = System.Drawing.Color.Empty;
            this.dSkinButton54.PressedImage = null;
            this.dSkinButton54.Radius = 10;
            this.dSkinButton54.ShowButtonBorder = true;
            this.dSkinButton54.Size = new System.Drawing.Size(105, 29);
            this.dSkinButton54.TabIndex = 45;
            this.dSkinButton54.Text = "关闭";
            this.dSkinButton54.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.dSkinButton54.TextPadding = 0;
            this.dSkinButton54.Click += new System.EventHandler(this.dSkinButton54_Click);
            // 
            // ExtractRtmpPushUrlForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(800, 331);
            this.Controls.Add(this.dSkinButton54);
            this.Controls.Add(this.dSkinButton42);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ExtractRtmpPushUrlForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "提取推流码";
            this.Load += new System.EventHandler(this.ExtractRtmpPushUrlForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private DSkin.Controls.DSkinButton dSkinButton42;
        private System.Windows.Forms.TextBox textBox1;
        private DSkin.Controls.DSkinButton dSkinButton54;
    }
}