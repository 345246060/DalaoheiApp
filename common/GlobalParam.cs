using System.Xml.Linq;

namespace common
{
    /// <summary>
    /// 应用程序全局静态常量
    /// </summary>
    public static class GlobalParam
    {
        #region 自动更新参数
        /// <summary>
        /// 是否检查自动更新：默认是true
        /// </summary>
        public static string CheckAutoUpdate = "true";

        /// <summary>
        /// 本地自动更新配置XML文件名
        /// </summary>
        public const string AutoUpdateConfig_XmlFileName = "Dalaohei_AutoUpdateConfig.xml";

        /// <summary>
        /// 本地自动更新下载临时存放目录
        /// </summary>
        public const string TempDir = "Temp";

        /// <summary>
        /// 远端自动更新信息XML文件名
        /// </summary>
        public const string AutoUpdateInfo_XmlFileName = "Dalaohei_AutoUpdateInfo.xml";

        /// <summary>
        /// 远端自动更新文件存放目录
        /// </summary>
        public const string RemoteDir = "Dalaohei_AutoUpdateFiles";

        /// <summary>
        /// 主线程名
        /// </summary>
        public const string MainProcess = "DalaoheiApp";
        #endregion

        /// <summary>
        /// 获取远端自动更新信息的版本号
        /// </summary>
        /// <returns></returns>
        public static string GetRemoteAutoUpdateInfoVersion(string xml_str)
        {
            XDocument doc = new XDocument();
            doc = XDocument.Parse(xml_str);
            return doc.Element("AutoUpdateInfo").Element("NewVersion").Value;
        }
    }
}