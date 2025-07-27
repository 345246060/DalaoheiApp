using CCWin;
using common;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace DalaoheiApp
{
    public partial class Verify_Form : CCSkinMain
    {
        public delegate void ResultReturnedHandler(string message);
        public event ResultReturnedHandler OnResultReturned;
        public Verify_Form()
        {
            InitializeComponent();
        }  
        private async void Verify_Form_Load(object sender, EventArgs es)
        {
            string Cache_user = GlobalData.user_code;
            string _userDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", Cache_user);
            Directory.CreateDirectory(_userDataFolder);
            var env = await CoreWebView2Environment.CreateAsync(
                null,
                _userDataFolder,
                options: new CoreWebView2EnvironmentOptions()
           );
            await webView21.EnsureCoreWebView2Async(env);
            await webView21.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
            await webView21.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.setExtraHTTPHeaders", @"
                    {
                      ""headers"": {
                        ""Accept-Language"": ""zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6"",
                        ""sec-ch-ua-form-factors"":""Desktop""
                      }
                    }");

            string spoofLangJs = @"
                    Object.defineProperty(navigator, 'languages', {
                      get: () => ['zh-CN', 'en', 'en-GB', 'en-US']
                    });
                    Object.defineProperty(navigator, 'language', {
                      get: () => 'zh-CN'
                    });
                    ";
            await webView21.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(spoofLangJs);
            await webView21.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Network.setUserAgentOverride",
                @"{
                        ""userAgent"": ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36 Edg/138.0.0.0"",
                        ""acceptLanguage"": ""zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6"",
                        ""platform"": ""Windows"",
                        ""userAgentMetadata"": {
                            ""brands"": [
                                {""brand"": ""Not)A;Brand"", ""version"": ""8""},
                                {""brand"": ""Chromium"", ""version"": ""138""},
                                {""brand"": ""Microsoft Edge"", ""version"": ""138""}
                            ],
                            ""fullVersionList"": [
                                {""brand"": ""Not)A;Brand"", ""version"": ""8.0.0.0""},
                                {""brand"": ""Chromium"", ""version"": ""138.0.3351.95""},
                                {""brand"": ""Microsoft Edge"", ""version"": ""138.0.3351.95""}
                            ],
                            ""fullVersion"": ""138.0.3351.95"",
                            ""platform"": ""Windows"",
                            ""platformVersion"": ""10.0.0"",
                            ""architecture"": ""x86"",
                            ""model"": """",
                            ""mobile"": false,
                            ""bitness"": ""64"",
                            ""wow64"": false
                        }
                    }"
            );
            await webView21.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
                Object.defineProperty(navigator, 'userAgentData', {
                    get: () => ({
                        brands: [
                            { brand: 'Not)A;Brand', version: '8' },
                            { brand: 'Chromium', version: '138' },
                            { brand: 'Microsoft Edge', version: '138' }
                        ],
                        mobile: false, 
                        getHighEntropyValues: async function(hints) {
                            const data = {
                                brands: [
                                    { brand: 'Not)A;Brand', version: '8' },
                                    { brand: 'Chromium', version: '138' },
                                    { brand: 'Microsoft Edge', version: '138' }
                                ],
                                fullVersionList: [
                                    { brand: 'Not)A;Brand', version: '8.0.0.0' },
                                    { brand: 'Chromium', version: '138.0.3351.95' },
                                    { brand: 'Microsoft Edge', version: '138.0.3351.95' }
                                ],
                                uaFullVersion: '138.0.3351.95',
                                platform: 'Windows',
                                platformVersion: '10.0.0',
                                architecture: 'x86',
                                model: '',
                                mobile: false,
                                bitness: '64',
                                wow64: false,
                                formFactors: ['desktop']
                            };
                            return Object.fromEntries(hints.map(h => [h, data[h]]));
                        }
                    })
                });
                ");
            // 5. 设置浏览器行为参数
            webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            webView21.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView21.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView21.CoreWebView2.Settings.IsZoomControlEnabled = true;
            webView21.CoreWebView2.Navigate("https://www.douyin.com/user/self?from_tab_name=main&showTab=like");
            
            webView21.CoreWebView2.WebResourceResponseReceived += async (s, e) =>
            {
                // 安全检查响应有效性
                if (e.Response == null || e.Response.StatusCode != 200)
                    return;

                var requestUri = e.Request.Uri;
                if (requestUri.Contains("message/get_user_message"))
                {
                    var ret_cookie = await utility.GetCookieHeaderForUrlAsync(webView21, "https://www.douyin.com/");
                    string cookie_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", GlobalData.user_code);
                    CookieStorage.WriteCookie(ret_cookie, cookie_path + "cookie.txt");
                    utility.logs($"心跳监测成功！"); 
                } 
            }; 
        }

        private void Verify_Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (webView21 != null)
                {
                    // 移除事件，防止内存泄漏
                    if (webView21.CoreWebView2 != null)
                    {
                        webView21.CoreWebView2.Stop(); // 停止加载
                    } 
                    webView21.Dispose(); // 释放控件资源
                    webView21 = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("关闭 WebView2 时出错：" + utility.RemoveUrls(ex.Message));
            }
        }

        private async void dSkinButton42_Click(object sender, EventArgs e)
        {
            try
            {
                string html = await webView21.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                html = System.Text.Json.JsonSerializer.Deserialize<string>(html); // 还原字符串（去除转义）
                                                                                  // 提取 shortId
                var match1 = Regex.Match(html, @"\\\""shortId\\\""\s*:\s*\\\""(.*?)\\\""");
                // 提取 nickname
                var match2 = Regex.Match(html, @"\\\""nickname\\\""\s*:\s*\\\""(.*?)\\\""");
                var match3 = Regex.Match(html, @"\\\""avatarUrl\\\""\s*:\s*\\\""(.*?)\\\""");
                string shortId = match1.Success ? match1.Groups[1].Value : "";
                string nickname = match2.Success ? match2.Groups[1].Value : "";
                string avatarUrl = match3.Success ? match3.Groups[1].Value : "";
                if (!string.IsNullOrWhiteSpace(shortId) && !string.IsNullOrWhiteSpace(nickname) && !string.IsNullOrWhiteSpace(avatarUrl))
                {
                    GlobalData.short_img = avatarUrl;
                    GlobalData.short_id = shortId;
                    GlobalData.nickname = nickname;
                    this.Hide();
                    utility.logs($"授权成功！");
                    Tooltip.ShowSuccess("授权成功！");
                    var ret_cookie = await utility.GetCookieHeaderForUrlAsync(webView21, "https://www.douyin.com/");
                    string cookie_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Cache", GlobalData.user_code);
                    CookieStorage.WriteCookie(ret_cookie, cookie_path + "cookie.txt");

                    OnResultReturned?.Invoke(GlobalData.short_id);  // 回调 
                }
                else
                {
                    utility.logs($"授权失败，请刷新页面或者退出重新登录！");
                    Tooltip.ShowError("授权失败，请刷新页面或者退出重新登录！", 3000);
                }
            }
            catch
            {
                Tooltip.ShowError("授权失败，请刷新页面或者退出重新登录 error！", 3000);
            }
        }
    }
}
