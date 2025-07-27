using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace common
{

    public class ListBoxWriter : TextWriter
    {
        private readonly ListBox _listBox;
        private readonly int _maxItems;

        public ListBoxWriter(ListBox listBox, int maxItems = 5000)
        {
            _listBox = listBox;
            _maxItems = maxItems;
        }

        // 使用UTF-8编码
        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value)
        {
            try
            {
                if (_listBox.InvokeRequired)
                {
                    _listBox.BeginInvoke(new Action(() => AddItem(value)));
                }
                else
                {
                    AddItem(value);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ListBoxWriter Exception: {ex.Message}");
            }
        }

        private void AddItem(string value)
        {
            try
            {
                // 保证日志不超过最大条数限制
                if (_listBox.Items.Count >= _maxItems)
                {
                    // 删除最旧的日志项
                    _listBox.Items.RemoveAt(0);
                }

                _listBox.Items.Add(value);
                _listBox.TopIndex = _listBox.Items.Count - 1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddItem Exception: {ex.Message}");
            }
        }
    }
}
