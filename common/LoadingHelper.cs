using DalaoheiApp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace common
{
    public class LoadingHelper
    {
        private static LoadingForm loadingForm = null; 
        // 显示模态加载窗口的方法
        public static void ShowLoading(Form parentForm, Control tabPage1)
        {
            if (loadingForm == null)
            {
                loadingForm = new LoadingForm();
                // 计算加载窗口的居中位置
                CenterLoadingForm(parentForm);

                SetControlsEnabled(tabPage1, false);

                // 显示加载窗口
                loadingForm.Show(parentForm);
            }

            //if (loadingForm == null)
            //{
            //    loadingForm = new LoadingForm();
            //    // 计算加载窗口的居中位置
            //    CenterLoadingForm(parentForm);

            //    // 使用异步方式显示模态窗口
            //    Task.Run(() =>
            //    {
            //        parentForm.Invoke(new Action(() =>
            //        {
            //            loadingForm.ShowDialog(parentForm);
            //        }));
            //    });
            //}
        }
        private static void SetControlsEnabled(Control parentControl, bool enabled)
        {
            // 遍历 parentControl 的所有子控件，并设置 Enabled 状态
            foreach (Control control in parentControl.Controls)
            {
                control.Enabled = enabled;
            }
        }
        // 隐藏加载窗口的方法
        public static void HideLoading(Form parentForm, Control tabPage1)
        {
            SetControlsEnabled(tabPage1, true);
            if (loadingForm != null)
            {
                loadingForm.Hide();
                loadingForm.Dispose();
                loadingForm = null;
            }

            //if (loadingForm != null)
            //{
            //    // 使用 Invoke 确保是在 UI 线程上关闭窗体
            //    loadingForm.Invoke(new Action(() =>
            //    {
            //        loadingForm.Hide();
            //        loadingForm.Dispose();
            //        loadingForm = null; 
            //    }));
            //}
        }

        // 让加载窗口始终居中于父窗体
        private static void CenterLoadingForm(Form parentForm)
        {
            if (loadingForm != null)
            {
                int x = parentForm.Location.X + (parentForm.Width - loadingForm.Width) / 2;
                int y = parentForm.Location.Y + (parentForm.Height - loadingForm.Height) / 2;
                loadingForm.Location = new Point(x, y);
            }
        }

        // 封装数据加载任务，执行异步数据处理
        public static async Task ExecuteWithLoading(Form parentForm, RichTextBox RichTextBox, Control tabPage1, Func<Task> task)
        {
            try
            {
                // 显示加载窗口
                ShowLoading(parentForm, tabPage1);

                // 执行数据处理任务
                await task();
            }
            finally
            {
                // 处理完后隐藏加载窗口
                HideLoading(parentForm, tabPage1);
                MoveToRichTextBoxEnd(RichTextBox);
            }
        }
        // 移动光标到 RichTextBox 的最后一行
        static void MoveToRichTextBoxEnd(RichTextBox skinRichTextBox1)
        {
            // 设置光标位置到文本的最后
            skinRichTextBox1.SelectionStart = skinRichTextBox1.Text.Length;
            // 滚动到光标所在的位置
            skinRichTextBox1.ScrollToCaret();
        }
    }
}
