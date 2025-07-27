using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace common
{
    /// <summary>
    /// 本地cookie数据操作类
    /// </summary>
    public class CookieStorage
    { 
        private static readonly ReaderWriterLockSlim fileLock = new ReaderWriterLockSlim();

        // 写入 Cookie（覆盖模式）
        public static void WriteCookie(string content,string outer_id)
        {
            try
            { 
                string cookie_path = Path.Combine(Path.GetTempPath(), "Cache", outer_id);
                Directory.CreateDirectory(cookie_path);
                fileLock.EnterWriteLock();
                File.WriteAllText(cookie_path + "\\cookie.txt", content, Encoding.UTF8);
            }
            finally
            {
                fileLock.ExitWriteLock();
            }
        }

        // 追加写入 Cookie（附加新记录）
        public static void AppendCookie(string content ,string CookieFilePath)
        {
            try
            {
                fileLock.EnterWriteLock();
                File.AppendAllText(CookieFilePath, content + Environment.NewLine, Encoding.UTF8);
            }
            finally
            {
                fileLock.ExitWriteLock();
            }
        }

        // 读取 Cookie 内容
        public static string ReadCookie(string outer_id)
        {
            try
            {
                string cookie_path = Path.Combine(Path.GetTempPath(), "Cache", outer_id);
                Directory.CreateDirectory(cookie_path);
                fileLock.EnterReadLock();
                return File.Exists(cookie_path + "\\cookie.txt")
                    ? File.ReadAllText(cookie_path + "\\cookie.txt", Encoding.UTF8)
                    : string.Empty;
            }
            finally
            {
                fileLock.ExitReadLock();
            }
        }
    }
}
