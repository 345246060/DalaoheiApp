
namespace DalaoheiApp
{
    partial class Verify_Form
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Verify_Form));
            this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.dSkinButton42 = new DSkin.Controls.DSkinButton();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // webView21
            // 
            this.webView21.AllowExternalDrop = true;
            this.webView21.CreationProperties = null;
            this.webView21.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView21.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webView21.Location = new System.Drawing.Point(3, 17);
            this.webView21.Name = "webView21";
            this.webView21.Size = new System.Drawing.Size(1127, 597);
            this.webView21.TabIndex = 3;
            this.webView21.ZoomFactor = 1D;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.dSkinButton42);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(7, 31);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1133, 54);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.DarkRed;
            this.label1.Location = new System.Drawing.Point(26, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(152, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "温馨提示：暂时没有";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.webView21);
            this.groupBox2.Location = new System.Drawing.Point(7, 91);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1133, 617);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            // 
            // dSkinButton42
            // 
            this.dSkinButton42.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.dSkinButton42.BaseColor = System.Drawing.Color.ForestGreen;
            this.dSkinButton42.ButtonBorderWidth = 1;
            this.dSkinButton42.Cursor = System.Windows.Forms.Cursors.Hand;
            this.dSkinButton42.DialogResult = System.Windows.Forms.DialogResult.None;
            this.dSkinButton42.ForeColor = System.Drawing.Color.White;
            this.dSkinButton42.HoverColor = System.Drawing.Color.Empty;
            this.dSkinButton42.HoverImage = null;
            this.dSkinButton42.Location = new System.Drawing.Point(1018, 15);
            this.dSkinButton42.Name = "dSkinButton42";
            this.dSkinButton42.NormalImage = null;
            this.dSkinButton42.PressColor = System.Drawing.Color.Empty;
            this.dSkinButton42.PressedImage = null;
            this.dSkinButton42.Radius = 10;
            this.dSkinButton42.ShowButtonBorder = true;
            this.dSkinButton42.Size = new System.Drawing.Size(102, 29);
            this.dSkinButton42.TabIndex = 38;
            this.dSkinButton42.Text = "登录好了";
            this.dSkinButton42.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.dSkinButton42.TextPadding = 0;
            this.dSkinButton42.Click += new System.EventHandler(this.dSkinButton42_Click);
            // 
            // Verify_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1147, 715);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.EffectCaption = CCWin.TitleType.Title;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Verify_Form";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "登录";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Verify_Form_FormClosing);
            this.Load += new System.EventHandler(this.Verify_Form_Load);
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private DSkin.Controls.DSkinButton dSkinButton42;
    }
}