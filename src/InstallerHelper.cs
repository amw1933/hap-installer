using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace HapInstaller
{
    public static class HuaweiApi
    {
        public static string AccessToken = "";
        public static string UserId = "";
        public static string JwtToken = "";

        public static string AccountFile = "";
        private static string DebugLogPath = "";

        public static void DebugLog(string msg)
        {
            try
            {
                if (DebugLogPath.Length == 0)
                {
                    string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
                    DebugLogPath = Path.Combine(exeDir, "login_debug.log");
                }
                File.AppendAllText(DebugLogPath, DateTime.Now.ToString("HH:mm:ss") + "  " + msg + "\r\n");
            }
            catch { }
        }

        public static readonly string[] DefaultAcl = {
            "ohos.permission.SYSTEM_FLOAT_WINDOW","ohos.permission.READ_CONTACTS","ohos.permission.WRITE_CONTACTS",
            "ohos.permission.READ_AUDIO","ohos.permission.WRITE_AUDIO","ohos.permission.READ_IMAGEVIDEO",
            "ohos.permission.WRITE_IMAGEVIDEO","ohos.permission.READ_WRITE_DESKTOP_DIRECTORY","ohos.permission.ACCESS_DDK_USB",
            "ohos.permission.ACCESS_DDK_HID","ohos.permission.READ_PASTEBOARD","ohos.permission.FILE_ACCESS_PERSIST",
            "ohos.permission.INTERCEPT_INPUT_EVENT","ohos.permission.INPUT_MONITORING","ohos.permission.SHORT_TERM_WRITE_IMAGEVIDEO",
            "ohos.permission.READ_WRITE_USER_FILE","ohos.permission.READ_WRITE_USB_DEV","ohos.permission.GET_WIFI_PEERS_MAC",
            "ohos.permission.SET_TELEPHONY_ESIM_STATE_OPEN","ohos.permission.kernel.DISABLE_CODE_MEMORY_PROTECTION",
            "ohos.permission.kernel.ALLOW_WRITABLE_CODE_MEMORY","ohos.permission.kernel.ALLOW_EXECUTABLE_FORT_MEMORY",
            "ohos.permission.MANAGE_PASTEBOARD_APP_SHARE_OPTION","ohos.permission.MANAGE_UDMF_APP_SHARE_OPTION",
            "ohos.permission.ACCESS_DISK_PHY_INFO","ohos.permission.PRELOAD_FILE","ohos.permission.SET_PAC_URL",
            "ohos.permission.PERSONAL_MANAGE_RESTRICTIONS","ohos.permission.START_PROVISIONING_MESSAGE",
            "ohos.permission.USE_FRAUD_CALL_LOG_PICKER","ohos.permission.USE_FRAUD_MESSAGES_PICKER",
            "ohos.permission.PERSISTENT_BLUETOOTH_PEERS_MAC","ohos.permission.ACCESS_VIRTUAL_SCREEN",
            "ohos.permission.MANAGE_APN_SETTING","ohos.permission.GET_WIFI_LOCAL_MAC",
            "ohos.permission.kernel.ALLOW_USE_JITFORT_INTERFACE","ohos.permission.GET_ETHERNET_LOCAL_MAC",
            "ohos.permission.kernel.DISABLE_GOTPLT_RO_PROTECTION","ohos.permission.USE_FRAUD_APP_PICKER",
            "ohos.permission.ACCESS_DDK_DRIVERS","ohos.permission.ACCESS_DDK_SCSI_PERIPHERAL",
            "ohos.permission.kernel.SUPPORT_PLUGIN","ohos.permission.CUSTOM_SANDBOX",
            "ohos.permission.MANAGE_SCREEN_TIME_GUARD","ohos.permission.GET_ABILITY_INFO",
            "ohos.permission.ACCESS_FIDO2_ONLINEAUTH","ohos.permission.USE_FLOAT_BALL",
            "ohos.permission.DLP_GET_HIDE_STATUS","ohos.permission.READ_LOCAL_DEVICE_NAME",
            "ohos.permission.KEEP_BACKGROUND_RUNNING_SYSTEM","ohos.permission.LINKTURBO",
            "ohos.permission.ACCESS_NET_TRACE_INFO","ohos.permission.READ_WHOLE_CALENDAR",
            "ohos.permission.WRITE_WHOLE_CALENDAR","ohos.permission.SET_SYSTEMSHARE_APPLAUNCHTRUSTLIST",
            "ohos.permission.HOOK_KEY_EVENT","ohos.permission.WEB_NATIVE_MESSAGING",
            "ohos.permission.SUBSCRIBE_NOTIFICATION","ohos.permission.CUSTOM_SCREEN_RECORDING",
            "ohos.permission.GET_IP_MAC_INFO","ohos.permission.ACCESS_USER_FULL_DISK",
            "ohos.permission.kernel.LOAD_INDEPENDENT_LIBRARY","ohos.permission.CRYPTO_EXTENSION_REGISTER"
        };

        public static bool LoggedIn { get { return AccessToken.Length > 0 && UserId.Length > 0; } }

        public static string Request(string url, string method, string body,
            Dictionary<string, string> extraHeaders, out int httpCode)
        {
            int lastCode = 0;
            string lastResult = "";
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                lastResult = RequestOnce(url, method, body, extraHeaders, out lastCode);
                if (lastCode == 200) break;
                if (attempt < 3) System.Threading.Thread.Sleep(1500);
            }
            httpCode = lastCode;
            return lastResult;
        }

        private static string RequestOnce(string url, string method, string body,
            Dictionary<string, string> extraHeaders, out int httpCode)
        {
            httpCode = 0;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = method;
                req.Timeout = 15000;
                req.Headers["oauth2Token"] = AccessToken ?? "";
                req.Headers["teamId"] = UserId ?? "";
                req.Headers["uid"] = UserId ?? "";
                if (extraHeaders != null)
                {
                    foreach (KeyValuePair<string, string> kv in extraHeaders)
                        req.Headers[kv.Key] = kv.Value;
                }
                if (body != null)
                {
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    req.ContentType = "application/json";
                    req.ContentLength = data.Length;
                    using (Stream s = req.GetRequestStream()) s.Write(data, 0, data.Length);
                }
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    httpCode = (int)resp.StatusCode;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        return sr.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    httpCode = (int)((HttpWebResponse)ex.Response).StatusCode;
                    using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                        return sr.ReadToEnd();
                }
                return "{\"ret\":{\"code\":-1,\"msg\":\"" + ex.Message.Replace("\"", "'") + "\"}}";
            }
            catch (Exception ex)
            {
                httpCode = -1;
                return "{\"ret\":{\"code\":-1,\"msg\":\"" + ex.Message.Replace("\"", "'") + "\"}}";
            }
        }

        public static void LoadAccount()
        {
            try
            {
                if (AccountFile.Length == 0 || !File.Exists(AccountFile)) return;
                string json = File.ReadAllText(AccountFile, Encoding.UTF8);
                AccessToken = GetStr(json, "\"accessToken\"\\s*:\\s*\"([^\"]+)\"");
                UserId = GetStr(json, "\"userId\"\\s*:\\s*\"([^\"]+)\"");
                JwtToken = GetStr(json, "\"jwtToken\"\\s*:\\s*\"([^\"]+)\"");
            }
            catch { }
        }

        public static void SaveAccount()
        {
            try
            {
                string json = "{\"accessToken\":\"" + AccessToken + "\",\"userId\":\"" + UserId +
                    "\",\"teamId\":\"" + UserId + "\",\"jwtToken\":\"" + JwtToken + "\"}";
                File.WriteAllText(AccountFile, json, Encoding.UTF8);
            }
            catch { }
        }

        public static string GetStr(string json, string pattern)
        {
            Match m = Regex.Match(json, pattern);
            return m.Success ? m.Groups[1].Value : "";
        }

        public static List<string> GetAll(string json, string pattern)
        {
            List<string> result = new List<string>();
            foreach (Match m in Regex.Matches(json, pattern)) result.Add(m.Groups[1].Value);
            return result;
        }

        public static string Login(Action<string> log)
        {
            int port = 3333 + new Random().Next(1000);
            string tempToken = null;
            TcpListener listener = null;
            TcpListener listener8888 = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                try
                {
                    listener8888 = new TcpListener(IPAddress.Any, 8888);
                    listener8888.Start();
                }
                catch { }
                log("请在弹出的浏览器中登录华为账号...");
                log("本地回调地址: http://127.0.0.1:" + port + "/callback");
                if (listener8888 != null) log("同时监听 8888 端口（DevEco 默认回调端口）");
                Process.Start("https://cn.devecostudio.huawei.com/console/DevEcoIDE/apply?port=" + port +
                    "&appid=1007&code=20698961dd4f420c8b44f49010c6f0cc");
                DateTime deadline = DateTime.Now.AddSeconds(90);
                DateTime hintAt = DateTime.Now.AddSeconds(20);
                while (DateTime.Now < deadline && tempToken == null)
                {
                    if (DateTime.Now >= hintAt)
                    {
                        hintAt = DateTime.MaxValue;
                        log("仍未收到回调：请确认浏览器已跳转到 127.0.0.1:" + port + "/callback；");
                        log("若浏览器显示无法访问，请关闭系统代理（如 Clash 的系统代理开关）后重试。");
                    }
                    TcpListener ready = null;
                    if (listener.Pending()) ready = listener;
                    else if (listener8888 != null && listener8888.Pending()) ready = listener8888;
                    if (ready == null) { System.Threading.Thread.Sleep(200); continue; }
                    using (TcpClient client = ready.AcceptTcpClient())
                    using (NetworkStream stream = client.GetStream())
                    {
                        byte[] buf = new byte[16384];
                        int total = 0;
                        client.ReceiveTimeout = 3000;
                        try
                        {
                            while (total < buf.Length)
                            {
                                int n = stream.Read(buf, total, buf.Length - total);
                                if (n <= 0) break;
                                total += n;
                                if (!stream.DataAvailable)
                                {
                                    System.Threading.Thread.Sleep(150);
                                    if (!stream.DataAvailable) break;
                                }
                            }
                        }
                        catch { }
                        string requestText = Encoding.UTF8.GetString(buf, 0, total);
                        string ok = "登录成功！请返回";
                        byte[] resp = Encoding.UTF8.GetBytes(
                            "HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: " +
                            Encoding.UTF8.GetByteCount(ok) + "\r\nConnection: close\r\n\r\n" + ok);
                        stream.Write(resp, 0, resp.Length);
                        tempToken = ParseTempToken(requestText);
                        if (tempToken != null) log("已收到华为回调，正在换取登录令牌...");
                    }
                }
            }
            catch (Exception ex)
            {
                return "登录失败: " + ex.Message;
            }
            finally
            {
                if (listener != null) listener.Stop();
                if (listener8888 != null) listener8888.Stop();
            }
            if (tempToken == null) return "登录超时：未收到华为回调（浏览器需跳转到 127.0.0.1:" + port + "/callback）";
            return ExchangeTempToken(tempToken, log);
        }

        public static string ExchangeTempToken(string tempToken, Action<string> log)
        {
            int code;
            DebugLog("tempToken: " + tempToken);
            log("tempToken: " + (tempToken.Length > 12 ? tempToken.Substring(0, 12) + "..." : tempToken));
            log("正在换取 jwtToken...");
            string jwtResp = Request(
                "https://cn.devecostudio.huawei.com/authrouter/auth/api/temptoken/check?site=CN&tempToken=" +
                Uri.EscapeDataString(tempToken) + "&appid=1007&version=0.0.0", "GET", null, null, out code);
            string jwt = GetStr(jwtResp, "\"msg\"\\s*:\\s*\"([^\"]+)\"");
            DebugLog("temptoken/check code=" + code + " resp=" +
                (jwtResp.Length > 300 ? jwtResp.Substring(0, 300) : jwtResp));
            if (jwt.Length == 0)
            {
                // 华为接口可能直接返回裸 JWT 作为响应体
                string trimmed = jwtResp.Trim();
                if (trimmed.StartsWith("eyJ") && trimmed.IndexOf(' ') < 0 && !trimmed.Contains("{"))
                    jwt = trimmed;
            }
            if (jwt.Length == 0)
            {
                log("接口响应: " + (jwtResp.Length > 200 ? jwtResp.Substring(0, 200) : jwtResp));
                return "登录失败：tempToken 无效（" + GetStr(jwtResp, "\"msg\"\\s*:\\s*\"([^\"]+)\"") + "）";
            }

            log("正在获取账号信息...");
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers["refresh"] = "false";
            headers["jwtToken"] = jwt;
            string infoResp = Request(
                "https://cn.devecostudio.huawei.com/authrouter/auth/api/jwToken/check", "GET", null, headers, out code);
            AccessToken = GetStr(infoResp, "\"accessToken\"\\s*:\\s*\"([^\"]+)\"");
            UserId = GetStr(infoResp, "\"userId\"\\s*:\\s*\"([^\"]+)\"");
            JwtToken = jwt;
            if (AccessToken.Length == 0) return "登录失败：未获取到 accessToken（" + code + "）";
            SaveAccount();
            log("登录成功: userId=" + UserId);
            return "登录成功";
        }

        public static string Refresh(Action<string> log)
        {
            if (JwtToken.Length == 0) return "没有可刷新的 jwtToken";
            int code;
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers["refresh"] = "true";
            headers["jwtToken"] = JwtToken;
            log("正在刷新账号有效期...");
            string resp = Request(
                "https://cn.devecostudio.huawei.com/authrouter/auth/api/jwToken/check", "GET", null, headers, out code);
            string at = GetStr(resp, "\"accessToken\"\\s*:\\s*\"([^\"]+)\"");
            string uid = GetStr(resp, "\"userId\"\\s*:\\s*\"([^\"]+)\"");
            if (at.Length == 0)
            {
                log("刷新失败（token 可能已过期），将尝试使用原账号信息: " + GetStr(resp, "\"msg\"\\s*:\\s*\"([^\"]+)\""));
                return HuaweiApi.LoggedIn ? "已导入保存账号: " + UserId : "未找到可用账号";
            }
            AccessToken = at;
            UserId = uid;
            SaveAccount();
            return "已刷新并登录: " + uid;
        }

        public static string ParseTempToken(string text)
        {
            Match m = Regex.Match(text, "tempToken=([^&\\s\"']+)");
            if (m.Success) return Uri.UnescapeDataString(m.Groups[1].Value);
            m = Regex.Match(text, "\"tempToken\"\\s*:\\s*\"([^\"]+)\"");
            if (m.Success) return Uri.UnescapeDataString(m.Groups[1].Value);
            string trimmed = text.Trim();
            if (trimmed.Length > 8 && !trimmed.Contains("=") && !trimmed.Contains("{") &&
                !trimmed.Contains("://") && !trimmed.Contains("/callback") &&
                !trimmed.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(trimmed);
            return null;
        }

        public static string EnsureCert(string storeDir, Action<string> log, ref string certId)
        {
            int code;
            string listResp = Request(
                "https://connect-api.cloud.huawei.com/api/cps/harmony-cert-manage/v1/cert/list", "GET", null, null, out code);
            if (code != 200) return "获取证书列表失败 (" + code + ")";

            if (HasDebugCert(listResp))
            {
                log("账号中已有调试证书，将复用；请确保 store 目录里有配套的 hmos.p12（否则密钥不匹配会签名失败）");
            }
            // find debug cert (certType=1) in cert list
            string certBlock = "";
            foreach (Match m in Regex.Matches(listResp, "\\{[^{}]*\\}"))
            {
                if (m.Value.Contains("\"certType\":1"))
                {
                    certBlock = m.Value;
                    break;
                }
            }
            if (certBlock.Length > 0)
            {
                certId = GetStr(certBlock, "\"id\":\"([^\"]+)\"");
                string objId = GetStr(certBlock, "\"certObjectId\":\"([^\"]+)\"");
                log("已找到调试证书: " + certId);
                log("已复用账号中已有的调试证书；若签名提示密钥不匹配，请把配套的 hmos.p12 放到 store 目录");
                DownloadCert(storeDir, objId, log);
                return "";
            }
            // create cert with csr
            string csr = "";
            try { csr = File.ReadAllText(Path.Combine(storeDir, "hmos.csr"), Encoding.UTF8); }
            catch { return "缺少 hmos.csr，无法创建证书"; }
            var serializer = new JavaScriptSerializer();
            string body = serializer.Serialize(new Dictionary<string, object> {
                {"csr", csr}, {"certName", "hmos-debug"}, {"certType", 1}
            });
            string addResp = Request(
                "https://connect-api.cloud.huawei.com/api/cps/harmony-cert-manage/v1/cert/add", "POST", body, null, out code);
            string newId = GetStr(addResp, "\"id\":\"([^\"]+)\"");
            if (newId.Length == 0) return "证书创建失败: " + GetStr(addResp, "\"msg\"\\s*:\\s*\"([^\"]+)\"");
            certId = newId;
            string newObj = GetStr(addResp, "\"certObjectId\":\"([^\"]+)\"");
            log("已创建调试证书: " + certId);
            DownloadCert(storeDir, newObj, log);
            return "";
        }

        private static bool HasDebugCert(string listResp)
        {
            foreach (Match m in Regex.Matches(listResp, "\\{[^{}]*\\}"))
            {
                if (m.Value.Contains("\"certType\":1")) return true;
            }
            return false;
        }

        public static bool HasDebugCertOnAccount(Action<string> log)
        {
            int code;
            string listResp = Request(
                "https://connect-api.cloud.huawei.com/api/cps/harmony-cert-manage/v1/cert/list", "GET", null, null, out code);
            if (code != 200) return false;
            return HasDebugCert(listResp);
        }

        private static void DownloadCert(string storeDir, string objId, Action<string> log)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string body = serializer.Serialize(new Dictionary<string, object> { { "sourceUrls", objId } });
                int code;
                string urlResp = Request(
                    "https://connect-api.cloud.huawei.com/api/amis/app-manage/v1/objects/url/reapply", "POST", body, null, out code);
                string url = GetStr(urlResp, "\"newUrl\":\"([^\"]+)\"");
                if (url.Length == 0) return;
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(url, Path.Combine(storeDir, "hmos-debug.cer"));
                }
                log("调试证书已更新");
            }
            catch { }
        }

        public static List<string> EnsureDevice(string udid, Action<string> log)
        {
            int code;
            string listResp = Request(
                "https://connect-api.cloud.huawei.com/api/cps/device-manage/v1/device/list?start=1&pageSize=100&encodeFlag=0",
                "GET", null, null, out code);
            List<string> deviceIds = GetAll(listResp, "\"id\":\"([^\"]+)\"");
            bool registered = listResp.Contains("\"" + udid + "\"");
            if (!registered && udid.Length > 0)
            {
                var serializer = new JavaScriptSerializer();
                string body = serializer.Serialize(new Dictionary<string, object> {
                    {"deviceName", "hmos-device-" + (udid.Length > 10 ? udid.Substring(0, 10) : udid)},
                    {"udid", udid}, {"deviceType", 4}
                });
                string addResp = Request(
                    "https://connect-api.cloud.huawei.com/api/cps/device-manage/v1/device/add", "POST", body, null, out code);
                log("已注册设备 UDID: " + udid);
                listResp = Request(
                    "https://connect-api.cloud.huawei.com/api/cps/device-manage/v1/device/list?start=1&pageSize=100&encodeFlag=0",
                    "GET", null, null, out code);
                deviceIds = GetAll(listResp, "\"id\":\"([^\"]+)\"");
            }
            return deviceIds;
        }

        public static string CreateProfile(string bundle, List<string> acl, List<string> deviceIds, string certId,
            string storeDir, Action<string> log)
        {
            var serializer = new JavaScriptSerializer();
            string profileName = "hmos-debug_" + bundle.Replace('.', '_');
            string body = serializer.Serialize(new Dictionary<string, object> {
                {"provisionName", profileName},
                {"aclPermissionList", acl},
                {"deviceList", deviceIds},
                {"certList", new List<string> { certId }},
                {"packageName", bundle}
            });
            int code;
            string resp = Request(
                "https://connect-api.cloud.huawei.com/api/cps/provision-manage/v1/ide/test/provision/add",
                "POST", body, null, out code);
            string url = GetStr(resp, "\"provisionFileUrl\":\"([^\"]+)\"");
            if (url.Length == 0) return "Profile 创建失败: " + GetStr(resp, "\"msg\"\\s*:\\s*\"([^\"]+)\"");
            string target = Path.Combine(storeDir, bundle.Replace('.', '_') + ".p7b");
            using (WebClient wc = new WebClient()) wc.DownloadFile(url, target);
            log("Profile 已生成: " + target);
            return "";
        }
    }

    public class AppInfo
    {
        public string Bundle = "";
        public string VersionName = "";
        public string VersionCode = "";
        public int MinApi = 20;
        public string MainAbility = "EntryAbility";
        public List<string> Permissions = new List<string>();
    }

    public static class HapReader
    {
        public static AppInfo Read(string path)
        {
            try
            {
                using (ZipArchive zip = ZipFile.OpenRead(path))
                {
                    ZipArchiveEntry entry = zip.GetEntry("module.json");
                    if (entry == null) return null;
                    using (StreamReader reader = new StreamReader(entry.Open(), Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        AppInfo info = new AppInfo();
                        info.Bundle = Get(json, "bundleName");
                        info.VersionName = Get(json, "versionName");
                        info.VersionCode = Get(json, "versionCode");
                        info.MainAbility = Get(json, "mainElement");
                        if (info.MainAbility.Length == 0) info.MainAbility = "EntryAbility";
                        try
                        {
                            var ser = new JavaScriptSerializer();
                            var root = ser.Deserialize<Dictionary<string, object>>(json);
                            if (root != null && root.ContainsKey("module"))
                            {
                                Dictionary<string, object> module = root["module"] as Dictionary<string, object>;
                                if (module != null && module.ContainsKey("requestPermissions"))
                                {
                                    ArrayList perms = module["requestPermissions"] as ArrayList;
                                    if (perms != null)
                                    {
                                        foreach (object o in perms)
                                        {
                                            Dictionary<string, object> d = o as Dictionary<string, object>;
                                            if (d != null && d.ContainsKey("name"))
                                                info.Permissions.Add(Convert.ToString(d["name"]));
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                        string minApi = Get(json, "minAPIVersion");
                        int v;
                        if (int.TryParse(minApi, out v) && v >= 1000000)
                            info.MinApi = v % 1000000;
                        else if (int.TryParse(minApi, out v) && v > 0)
                            info.MinApi = v;
                        return info;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static string Get(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"?([^\"\\s,}]+)\"?");
            if (m.Success) return m.Groups[1].Value;
            return "";
        }
    }

    public static class ProcessRunner
    {
        public static string Run(string exe, string args, out int exitCode)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    string so = p.StandardOutput.ReadToEnd();
                    string se = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    exitCode = p.ExitCode;
                    return so + se;
                }
            }
            catch (Exception ex)
            {
                exitCode = -1;
                return "启动进程失败: " + ex.Message;
            }
        }
    }

    public class Settings
    {
        public string JavaPath = @"C:\Program Files\Huawei\DevEco Studio\jbr\bin\java.exe";
        public string HapSignTool = @"C:\Program Files\Huawei\DevEco Studio\sdk\default\openharmony\toolchains\lib\hap-sign-tool.jar";
        public string HdcPath = @"C:\Program Files\Huawei\DevEco Studio\sdk\default\openharmony\toolchains\hdc.exe";
        public string StoreDir = "";
        public string Keystore = "";
        public string Cert = "";
        public string Profile = "";
        public string KeystorePwd = "hmos123";
        public string KeyAlias = "hmos";
        public bool AutoProfile = true;
        public string LastHap = "";
        public string IniPath;

        public Settings()
        {
            IniPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath) ?? ".", "鸿蒙安装助手.ini");
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(IniPath)) return;
                foreach (string line in File.ReadAllLines(IniPath, Encoding.UTF8))
                {
                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;
                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();
                    switch (key)
                    {
                        case "java": JavaPath = val; break;
                        case "signTool": HapSignTool = val; break;
                        case "hdc": HdcPath = val; break;
                        case "store": StoreDir = val; break;
                        case "keystore": Keystore = val; break;
                        case "cert": Cert = val; break;
                        case "profile": Profile = val; break;
                        case "pwd": KeystorePwd = val; break;
                        case "alias": KeyAlias = val; break;
                        case "autoProfile": bool b; AutoProfile = bool.TryParse(val, out b) && b; break;
                        case "lastHap": LastHap = val; break;
                    }
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("java=" + JavaPath);
                sb.AppendLine("signTool=" + HapSignTool);
                sb.AppendLine("hdc=" + HdcPath);
                sb.AppendLine("store=" + StoreDir);
                sb.AppendLine("keystore=" + Keystore);
                sb.AppendLine("cert=" + Cert);
                sb.AppendLine("profile=" + Profile);
                sb.AppendLine("pwd=" + KeystorePwd);
                sb.AppendLine("alias=" + KeyAlias);
                sb.AppendLine("autoProfile=" + AutoProfile);
                sb.AppendLine("lastHap=" + LastHap);
                File.WriteAllText(IniPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public void AutoFix()
        {
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
            string portableJava = Path.Combine(exeDir, "jbr", "bin", "java.exe");
            string portableSignTool = Path.Combine(exeDir, "tools", "hap-sign-tool.jar");
            string portableHdc = Path.Combine(exeDir, "tools", "hdc.exe");
            string localStore = Path.Combine(exeDir, "store");
            if (File.Exists(portableJava)) JavaPath = portableJava;
            if (File.Exists(portableSignTool)) HapSignTool = portableSignTool;
            if (File.Exists(portableHdc)) HdcPath = portableHdc;
            if (Directory.Exists(localStore) && (StoreDir.Length == 0 || !Directory.Exists(StoreDir)))
                StoreDir = localStore;
            if (!File.Exists(Keystore) && File.Exists(Path.Combine(localStore, "hmos.p12")))
                Keystore = Path.Combine(localStore, "hmos.p12");
            if (!File.Exists(Cert) && File.Exists(Path.Combine(localStore, "hmos-debug.cer")))
                Cert = Path.Combine(localStore, "hmos-debug.cer");
            if (!File.Exists(Profile) && File.Exists(Path.Combine(localStore, "hmos-debug.p7b")))
                Profile = Path.Combine(localStore, "hmos-debug.p7b");
            string[] hdcCandidates = { HdcPath };
            foreach (string c in hdcCandidates)
            {
                if (File.Exists(c)) { HdcPath = c; break; }
            }
            string[] javaCandidates = {
                JavaPath,
                @"C:\Program Files\Huawei\DevEco Studio\jbr\bin\java.exe",
                @"D:\Program Files\Huawei\DevEco Studio\jbr\bin\java.exe"
            };
            foreach (string c in javaCandidates)
            {
                if (File.Exists(c)) { JavaPath = c; break; }
            }
        }
    }

    public class RoundedButton : Button
    {
        public int Radius { get; set; }
        public Color NormalColor { get; set; }
        public Color HoverColor { get; set; }
        private bool hovered = false;

        public RoundedButton()
        {
            Radius = 12;
            NormalColor = Color.FromArgb(77, 159, 255);
            HoverColor = Color.FromArgb(102, 176, 255);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            ForeColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath path = RoundedRect(rect, Radius))
            using (SolidBrush brush = new SolidBrush(hovered ? HoverColor : NormalColor))
            {
                pevent.Graphics.FillPath(brush, path);
            }
            Color textColor = Enabled ? ForeColor : Color.FromArgb(140, 148, 165);
            TextRenderer.DrawText(pevent.Graphics, Text, Font, rect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedPanel : Panel
    {
        public int Radius { get; set; }
        public Color BorderColor { get; set; }

        public RoundedPanel()
        {
            Radius = 16;
            BorderColor = Color.FromArgb(46, 46, 64);
            BackColor = Color.FromArgb(28, 28, 40);
            Padding = new Padding(16);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (GraphicsPath path = RoundedButton.RoundedRect(
                new Rectangle(0, 0, Width - 1, Height - 1), Radius))
            {
                Region = new Region(path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = RoundedButton.RoundedRect(
                new Rectangle(0, 0, Width - 1, Height - 1), Radius))
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.DrawPath(pen, path);
            }
        }
    }

    public class LoginForm : Form
    {
        public string ErrorMessage = "";
        public Action<string> LogCallback = null;
        private WebView2 webView;
        private bool captured = false;
        private int port;
        private Label lblStatus;
        private TcpListener listener;

        public LoginForm()
        {
            port = 3333 + new Random().Next(1000);
            Text = "华为账号登录";
            ClientSize = new Size(940, 700);
            MinimumSize = new Size(820, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(22, 22, 30);
            Label hint = new Label();
            hint.Text = "请在窗口内登录华为账号，登录完成后会自动继续，无需手动复制 token";
            hint.Dock = DockStyle.Top;
            hint.Height = 34;
            hint.TextAlign = ContentAlignment.MiddleLeft;
            hint.Padding = new Padding(12, 0, 0, 0);
            hint.BackColor = Color.FromArgb(30, 30, 44);
            hint.ForeColor = Color.FromArgb(190, 196, 214);
            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 28;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Padding = new Padding(12, 0, 0, 0);
            lblStatus.BackColor = Color.FromArgb(26, 26, 38);
            lblStatus.ForeColor = Color.FromArgb(154, 162, 185);
            lblStatus.Text = "等待登录...";
            Controls.Add(webView);
            Controls.Add(hint);
            Controls.Add(lblStatus);
            FormClosed += OnFormClosed;
            Load += OnLoad;
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            try { if (listener != null) listener.Stop(); }
            catch { }
        }

        private void FormLog(string msg)
        {
            HuaweiApi.DebugLog(msg);
            try
            {
                if (LogCallback != null) LogCallback(msg);
            }
            catch { }
        }

        private void OnLoad(object sender, EventArgs e)
        {
            StartLocalListener();
            try
            {
                System.Threading.Tasks.Task task = webView.EnsureCoreWebView2Async(null);
                task.ContinueWith(t =>
                {
                    try
                    {
                        CoreWebView2 core = webView.CoreWebView2;
                        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                        core.WebResourceRequested += OnWebResourceRequested;
                        core.NavigationStarting += OnNavigationStarting;
                        core.NewWindowRequested += OnNewWindowRequested;
                        core.Navigate("https://cn.devecostudio.huawei.com/console/DevEcoIDE/apply?port=" + port +
                            "&appid=1007&code=20698961dd4f420c8b44f49010c6f0cc");
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = "WebView2 初始化失败: " + ex.Message;
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }
                }, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                ErrorMessage = "WebView2 初始化失败: " + ex.Message;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void StartLocalListener()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                System.Threading.Thread t = new System.Threading.Thread(() =>
                {
                    try
                    {
                        DateTime deadline = DateTime.Now.AddSeconds(150);
                        while (DateTime.Now < deadline && !captured)
                        {
                            if (!listener.Pending()) { System.Threading.Thread.Sleep(200); continue; }
                            using (TcpClient client = listener.AcceptTcpClient())
                            using (NetworkStream stream = client.GetStream())
                            {
                                byte[] buf = new byte[16384];
                                int total = 0;
                                try
                                {
                                    while (total < buf.Length)
                                    {
                                        int n = stream.Read(buf, total, buf.Length - total);
                                        if (n <= 0) break;
                                        total += n;
                                        if (!stream.DataAvailable)
                                        {
                                            System.Threading.Thread.Sleep(150);
                                            if (!stream.DataAvailable) break;
                                        }
                                    }
                                }
                                catch { }
                                string requestText = Encoding.UTF8.GetString(buf, 0, total);
                                HuaweiApi.DebugLog("监听收到请求: " +
                                    (requestText.Length > 300 ? requestText.Substring(0, 300) : requestText));
                                string ok = "登录成功！请返回";
                                byte[] resp = Encoding.UTF8.GetBytes(
                                    "HTTP/1.1 200 OK\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: " +
                                    Encoding.UTF8.GetByteCount(ok) + "\r\nConnection: close\r\n\r\n" + ok);
                                stream.Write(resp, 0, resp.Length);
                                string temp = HuaweiApi.ParseTempToken(requestText);
                                if (temp != null && !captured)
                                {
                                    captured = true;
                                    FormLog("本地监听捕获 tempToken: " +
                                        (temp.Length > 12 ? temp.Substring(0, 12) + "..." : temp));
                                    CompleteLogin(temp);
                                }
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        try { if (listener != null) listener.Stop(); }
                        catch { }
                    }
                });
                t.IsBackground = true;
                t.Start();
            }
            catch { }
        }

        private void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            try { webView.CoreWebView2.Navigate(e.Uri); }
            catch { }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (captured) return;
            if (e.Uri == null || !e.Uri.Contains("/callback")) return;
            string temp = HuaweiApi.ParseTempToken(e.Uri);
            if (temp != null)
            {
                FormLog("捕获到导航回调 tempToken: " + (temp.Length > 12 ? temp.Substring(0, 12) + "..." : temp));
                captured = true;
                e.Cancel = true;
                CompleteLogin(temp);
            }
        }

        private void OnWebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (captured) return;
            try
            {
                CoreWebView2WebResourceRequest req = e.Request;
                string reqUri = req.Uri ?? "";
                bool isCallback = reqUri.Contains("/callback") || reqUri.Contains("127.0.0.1") || reqUri.Contains("localhost");
                if (isCallback && req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) && req.Content != null)
                {
                    string body = ReadIStream(req.Content);
                    HuaweiApi.DebugLog("WebView2 拦截 POST url=" + reqUri + " body=" +
                        (body.Length > 300 ? body.Substring(0, 300) : body));
                    string temp = HuaweiApi.ParseTempToken(body);
                    if (temp != null)
                    {
                        FormLog("捕获到 POST 回调 tempToken: " + (temp.Length > 12 ? temp.Substring(0, 12) + "..." : temp) + " url=" + reqUri);
                        captured = true;
                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("ok")))
                        {
                            e.Response = webView.CoreWebView2.Environment.CreateWebResourceResponse(ms, 200, "OK", "");
                        }
                        CompleteLogin(temp);
                    }
                }
            }
            catch { }
        }

        private static string ReadIStream(System.IO.Stream stream)
        {
            try
            {
                if (stream == null) return "";
                stream.Position = 0;
                using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch { return ""; }
        }

        private void CompleteLogin(string tempToken)
        {
            SetStatus("正在登录，请稍候...");
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                string result = HuaweiApi.ExchangeTempToken(tempToken, FormLog);
                bool ok = HuaweiApi.LoggedIn;
                BeginInvoke(new Action(() =>
                {
                    if (ok)
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        ErrorMessage = result;
                        captured = false;
                        SetStatus("登录失败: " + result + "（可重试或关闭窗口）");
                    }
                }));
            });
            t.IsBackground = true;
            t.Start();
        }

        private void SetStatus(string msg)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action<string>(SetStatus), msg);
                    return;
                }
                lblStatus.Text = msg;
            }
            catch { }
        }
    }

    public class MainForm : Form
    {
        private Settings settings = new Settings();
        private TextBox txtHap;
        private TextBox txtInfo;
        private TextBox txtKeystore, txtCert, txtProfile, txtPwd, txtAlias, txtStore;
        private CheckBox chkAutoProfile;
        private TextBox txtLog;
        private Label lblDevice;
        private Label lblAccount;
        private TextBox txtIp;
        private TextBox txtPort;
        private TextBox txtTempToken;
        private AppInfo currentInfo;
        private bool dragging = false;
        private Point dragOffset;
        private bool loggingIn = false;
        private HashSet<string> aclBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MainForm()
        {
            settings.Load();
            settings.AutoFix();
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
            HuaweiApi.AccountFile = settings.StoreDir.Length > 0
                ? Path.Combine(settings.StoreDir, "userInfo.json")
                : Path.Combine(exeDir, "userInfo.json");
            InitUi();
            Log("欢迎使用鸿蒙 HAP 安装助手");
            Log("配置目录: " + settings.IniPath);
            Log("Java: " + (File.Exists(settings.JavaPath) ? settings.JavaPath : "未找到（需安装 DevEco Studio）"));
            Log("hdc: " + (File.Exists(settings.HdcPath) ? settings.HdcPath : "未找到"));
            Log("Profile目录: " + settings.StoreDir);
            if (settings.LastHap.Length > 0 && File.Exists(settings.LastHap))
            {
                txtHap.Text = settings.LastHap;
                LoadHap(settings.LastHap);
            }
        }

        private void InitUi()
        {
            Text = "鸿蒙 HAP 安装助手";
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(880, 780);
            MinimumSize = new Size(820, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(22, 22, 30);
            ForeColor = Color.FromArgb(232, 234, 240);
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            AllowDrop = true;
            DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            DragDrop += (s, e) => {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && files[0].ToLower().EndsWith(".hap"))
                {
                    txtHap.Text = files[0];
                    LoadHap(files[0]);
                }
            };

            Panel titleBar = new Panel();
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 46;
            titleBar.BackColor = Color.FromArgb(30, 30, 44);
            Label titleLabel = new Label();
            titleLabel.Text = "⚡ 鸿蒙 HAP 安装助手";
            titleLabel.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(77, 159, 255);
            titleLabel.Location = new Point(16, 11);
            titleLabel.AutoSize = true;
            Label titleSub = new Label();
            titleSub.Text = "通用版 · 签名 / 安装 / 账号";
            titleSub.Font = new Font("Microsoft YaHei UI", 8.5F);
            titleSub.ForeColor = Color.FromArgb(122, 130, 155);
            titleSub.Location = new Point(200, 15);
            titleSub.AutoSize = true;
            Button btnMin = MakeFlat("—", 806, 6, 34, 34, () => { WindowState = FormWindowState.Minimized; });
            Button btnClose = MakeFlat("✕", 844, 6, 34, 34, () => { Close(); });
            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(titleSub);
            titleBar.Controls.Add(btnMin);
            titleBar.Controls.Add(btnClose);
            titleBar.MouseDown += TitleBar_MouseDown;
            titleBar.MouseMove += TitleBar_MouseMove;
            titleBar.MouseUp += TitleBar_MouseUp;
            titleBar.MouseLeave += TitleBar_MouseLeave;
            titleLabel.MouseDown += TitleBar_MouseDown;
            titleLabel.MouseMove += TitleBar_MouseMove;
            titleLabel.MouseUp += TitleBar_MouseUp;
            titleLabel.MouseLeave += TitleBar_MouseLeave;
            Controls.Add(titleBar);

            int x = 16;
            int y = 58;
            int cardW = 848;

            RoundedPanel pDevice = new RoundedPanel();
            pDevice.Location = new Point(x, y);
            pDevice.Size = new Size(cardW, 82);
            pDevice.Controls.Add(Header("设备连接", 0, 0));
            lblDevice = Chip("未检测", Color.FromArgb(229, 72, 77), 500, 0);
            lblDevice.AutoSize = false;
            lblDevice.Width = 185;
            lblDevice.TextAlign = ContentAlignment.MiddleCenter;
            pDevice.Controls.Add(lblDevice);
            pDevice.Controls.Add(MakeBtn("连接检测", 700, 0, 96, 26, Color.FromArgb(58, 58, 76), Color.FromArgb(72, 72, 94), (s, e) => CheckDevice()));
            pDevice.Controls.Add(FieldLabel("IP", 16, 38));
            txtIp = MakeText(52, 38, 130, "");
            pDevice.Controls.Add(txtIp);
            pDevice.Controls.Add(FieldLabel("端口", 196, 38));
            txtPort = MakeText(238, 38, 90, "");
            pDevice.Controls.Add(txtPort);
            pDevice.Controls.Add(MakeBtn("无线连接", 344, 38, 100, 28, Color.FromArgb(77, 159, 255), Color.FromArgb(102, 176, 255), (s, e) => Tconn()));
            Controls.Add(pDevice);
            y += 92;

            RoundedPanel pHap = new RoundedPanel();
            pHap.Location = new Point(x, y);
            pHap.Size = new Size(cardW, 96);
            pHap.Controls.Add(Header("安装包", 0, 0));
            Label lDrop = new Label();
            lDrop.Text = "支持直接拖入 .hap 文件";
            lDrop.ForeColor = Color.FromArgb(122, 130, 155);
            lDrop.Font = new Font("Microsoft YaHei UI", 8.5F);
            lDrop.Location = new Point(120, 4);
            lDrop.AutoSize = true;
            pHap.Controls.Add(lDrop);
            txtHap = MakeText(16, 34, cardW - 236, "", true);
            pHap.Controls.Add(txtHap);
            pHap.Controls.Add(MakeBtn("浏览...", cardW - 190, 34, 90, 28, Color.FromArgb(58, 58, 76), Color.FromArgb(72, 72, 94), (s, e) => Browse()));
            txtInfo = new TextBox();
            txtInfo.Location = new Point(16, 70);
            txtInfo.Width = cardW - 32;
            txtInfo.BorderStyle = BorderStyle.None;
            txtInfo.ReadOnly = true;
            txtInfo.BackColor = pHap.BackColor;
            txtInfo.ForeColor = Color.FromArgb(154, 162, 185);
            txtInfo.Text = "拖入或选择 HAP 后自动读取包名/版本";
            pHap.Controls.Add(txtInfo);
            Controls.Add(pHap);
            y += 106;

            RoundedPanel pSign = new RoundedPanel();
            pSign.Location = new Point(x, y);
            pSign.Size = new Size(cardW, 192);
            pSign.Controls.Add(Header("签名配置", 0, 0));
            chkAutoProfile = new CheckBox();
            chkAutoProfile.Text = "自动匹配包名 Profile";
            chkAutoProfile.Checked = settings.AutoProfile;
            chkAutoProfile.ForeColor = Color.FromArgb(190, 196, 214);
            chkAutoProfile.BackColor = Color.Transparent;
            chkAutoProfile.Location = new Point(cardW - 220, 2);
            chkAutoProfile.AutoSize = true;
            pSign.Controls.Add(chkAutoProfile);
            int fy = 32;
            txtStore = AddField(pSign, "Profile目录", fy, settings.StoreDir, cardW - 130); fy += 30;
            txtKeystore = AddField(pSign, "密钥库", fy, settings.Keystore, cardW - 130); fy += 30;
            txtCert = AddField(pSign, "证书", fy, settings.Cert, cardW - 130); fy += 30;
            txtProfile = AddField(pSign, "Profile", fy, settings.Profile, cardW - 130); fy += 30;
            txtPwd = AddField(pSign, "密码", fy, settings.KeystorePwd, 240, true);
            txtAlias = AddField(pSign, "别名", fy, settings.KeyAlias, 240, false, 330);
            Controls.Add(pSign);
            y += 202;

            RoundedPanel pAccount = new RoundedPanel();
            pAccount.Location = new Point(x, y);
            pAccount.Size = new Size(cardW, 118);
            pAccount.Controls.Add(Header("华为账号", 0, 0));
            pAccount.Controls.Add(MakeBtn("华为账号登录", 16, 30, 130, 30, Color.FromArgb(77, 159, 255), Color.FromArgb(102, 176, 255), (s, e) => HuaweiLogin()));
            pAccount.Controls.Add(MakeBtn("生成Profile", 158, 30, 110, 30, Color.FromArgb(47, 191, 113), Color.FromArgb(66, 214, 136), (s, e) => GenProfile()));
            lblAccount = Chip(HuaweiApi.LoggedIn ? "已登录 " + HuaweiApi.UserId : "未登录",
                HuaweiApi.LoggedIn ? Color.FromArgb(47, 191, 113) : Color.FromArgb(229, 72, 77), 300, 32);
            lblAccount.AutoSize = false;
            lblAccount.Width = 340;
            lblAccount.TextAlign = ContentAlignment.MiddleLeft;
            pAccount.Controls.Add(lblAccount);
            pAccount.Controls.Add(FieldLabel("tempToken", 16, 70));
            txtTempToken = MakeText(96, 70, 430, "");
            pAccount.Controls.Add(txtTempToken);
            pAccount.Controls.Add(MakeBtn("用token登录", 540, 70, 120, 30, Color.FromArgb(240, 173, 78), Color.FromArgb(245, 190, 105), (s, e) => ManualLogin()));
            pAccount.Controls.Add(MakeBtn("导入已保存账号", 670, 70, 140, 30, Color.FromArgb(88, 101, 242), Color.FromArgb(110, 122, 255), (s, e) => ImportSaved()));
            Controls.Add(pAccount);
            y += 128;

            RoundedPanel pAction = new RoundedPanel();
            pAction.Location = new Point(x, y);
            pAction.Size = new Size(cardW, 70);
            pAction.Controls.Add(Header("操作", 0, 0));
            pAction.Controls.Add(MakeBtn("① 签名并安装", 16, 30, 150, 32, Color.FromArgb(77, 159, 255), Color.FromArgb(102, 176, 255), (s, e) => SignAndInstall(true)));
            pAction.Controls.Add(MakeBtn("② 仅安装", 176, 30, 100, 32, Color.FromArgb(58, 58, 76), Color.FromArgb(72, 72, 94), (s, e) => InstallOnly()));
            pAction.Controls.Add(MakeBtn("③ 卸载", 286, 30, 90, 32, Color.FromArgb(229, 72, 77), Color.FromArgb(235, 100, 100), (s, e) => Uninstall()));
            pAction.Controls.Add(MakeBtn("④ 启动", 386, 30, 90, 32, Color.FromArgb(58, 58, 76), Color.FromArgb(72, 72, 94), (s, e) => LaunchApp()));
            pAction.Controls.Add(MakeBtn("Profile目录", 486, 30, 110, 32, Color.FromArgb(58, 58, 76), Color.FromArgb(72, 72, 94), (s, e) => OpenStoreDir()));
            pAction.Controls.Add(MakeBtn("保存配置", 606, 30, 100, 32, Color.FromArgb(58, 58, 76), Color.FromArgb(72, 72, 94), (s, e) => SaveCfg()));
            Controls.Add(pAction);
            y += 80;

            RoundedPanel pLog = new RoundedPanel();
            pLog.Location = new Point(x, y);
            pLog.Size = new Size(cardW, ClientSize.Height - y - 16);
            pLog.Controls.Add(Header("运行日志", 0, 0));
            txtLog = new TextBox();
            txtLog.Location = new Point(16, 26);
            txtLog.Width = cardW - 32;
            txtLog.Height = pLog.Height - 42;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BackColor = Color.FromArgb(16, 16, 22);
            txtLog.ForeColor = Color.FromArgb(112, 228, 160);
            txtLog.BorderStyle = BorderStyle.FixedSingle;
            txtLog.Font = new Font("Consolas", 9F);
            pLog.Controls.Add(txtLog);
            Controls.Add(pLog);
        }

        private Button MakeFlat(string text, int x, int y, int w, int h, Action onClick)
        {
            Button b = new Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(30, 30, 44);
            b.ForeColor = Color.FromArgb(200, 205, 220);
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 64, 88);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(48, 48, 70);
            b.Click += (s, e) => onClick();
            return b;
        }

        private Label Header(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            l.ForeColor = Color.FromArgb(225, 229, 240);
            l.Location = new Point(x, y);
            l.AutoSize = true;
            return l;
        }

        private Label FieldLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.ForeColor = Color.FromArgb(154, 162, 185);
            l.Font = new Font("Microsoft YaHei UI", 8.5F);
            l.Location = new Point(x, y + 4);
            l.AutoSize = true;
            return l;
        }

        private Label Chip(string text, Color color, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.ForeColor = color;
            l.Font = new Font("Microsoft YaHei UI", 9F);
            l.Location = new Point(x, y);
            l.AutoSize = true;
            l.Padding = new Padding(10, 4, 10, 4);
            l.BackColor = Color.FromArgb(38, 38, 54);
            return l;
        }

        private TextBox MakeText(int x, int y, int w, string text, bool readOnly = false)
        {
            TextBox t = new TextBox();
            t.Location = new Point(x, y);
            t.Width = w;
            t.Text = text;
            t.BackColor = Color.FromArgb(19, 19, 28);
            t.ForeColor = Color.FromArgb(232, 234, 240);
            t.BorderStyle = BorderStyle.FixedSingle;
            t.ReadOnly = readOnly;
            return t;
        }

        private RoundedButton MakeBtn(string text, int x, int y, int w, int h, Color normal, Color hover, EventHandler onClick)
        {
            RoundedButton b = new RoundedButton();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.NormalColor = normal;
            b.HoverColor = hover;
            b.Click += onClick;
            return b;
        }

        private TextBox AddField(RoundedPanel parent, string label, int y, string value, int width, bool password = false, int x = 0)
        {
            Label l = FieldLabel(label, 16 + x, y);
            TextBox t = MakeText(96 + x, y, width, value);
            if (password) t.UseSystemPasswordChar = true;
            parent.Controls.Add(l);
            parent.Controls.Add(t);
            return t;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                dragOffset = e.Location;
                Control c = sender as Control;
                if (c != null) c.Capture = true;
            }
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging && e.Button == MouseButtons.Left)
            {
                Point p = PointToScreen(e.Location);
                Location = new Point(p.X - dragOffset.X, p.Y - dragOffset.Y);
            }
        }

        private void TitleBar_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
            Control c = sender as Control;
            if (c != null) c.Capture = false;
        }

        private void TitleBar_MouseLeave(object sender, EventArgs e)
        {
            Control c = sender as Control;
            if (c != null && !c.Capture) dragging = false;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            dragging = false;
            base.OnMouseUp(e);
        }

        private void Log(string msg)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(Log), msg); }
                catch { }
                return;
            }
            if (txtLog == null) return;
            try { txtLog.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + msg + "\r\n"); }
            catch { }
        }

        private void Browse()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "HAP 文件 (*.hap)|*.hap|所有文件 (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtHap.Text = dlg.FileName;
                    LoadHap(dlg.FileName);
                }
            }
        }

        private void LoadHap(string path)
        {
            currentInfo = HapReader.Read(path);
            if (currentInfo == null || currentInfo.Bundle.Length == 0)
            {
                txtInfo.Text = "无法读取包信息（可能不是有效的 HAP）";
                Log("读取失败: " + path);
                return;
            }
            txtInfo.Text = string.Format("包名: {0}   版本: {1} (code {2})   主Ability: {3}   MinAPI: {4}",
                currentInfo.Bundle, currentInfo.VersionName, currentInfo.VersionCode, currentInfo.MainAbility, currentInfo.MinApi);
            settings.LastHap = path;
            Log("读取成功: " + currentInfo.Bundle + " " + currentInfo.VersionName);
            if (chkAutoProfile.Checked)
            {
                string auto = FindProfile(currentInfo.Bundle);
                if (auto != null)
                {
                    txtProfile.Text = auto;
                    Log("自动匹配 Profile: " + auto);
                }
                else
                {
                    Log("未找到 " + currentInfo.Bundle + " 的专属 Profile，将使用默认 Profile（包名不匹配时签名会失败）");
                }
            }
        }

        private string FindProfile(string bundle)
        {
            try
            {
                string key = bundle.Replace('.', '_');
                string file = Path.Combine(txtStore.Text, key + ".p7b");
                if (File.Exists(file)) return file;
                foreach (string f in Directory.GetFiles(txtStore.Text, "*.p7b"))
                {
                    if (Path.GetFileNameWithoutExtension(f).Replace('_', '.').IndexOf(bundle, StringComparison.OrdinalIgnoreCase) >= 0)
                        return f;
                }
            }
            catch { }
            return null;
        }

        private void CheckDevice()
        {
            if (!File.Exists(settings.HdcPath)) { Log("找不到 hdc: " + settings.HdcPath); return; }
            int exit;
            string outText = ProcessRunner.Run(settings.HdcPath, "list targets", out exit);
            outText = outText.Trim();
            if (outText.Length == 0 || outText.Contains("[Empty]"))
            {
                lblDevice.Text = "未连接";
                lblDevice.ForeColor = Color.DarkRed;
                Log("未检测到设备");
            }
            else
            {
                lblDevice.Text = outText.Split('\n')[0].Trim();
                lblDevice.ForeColor = Color.Green;
                Log("已连接设备: " + lblDevice.Text);
            }
        }

        private void Tconn()
        {
            string ip = txtIp.Text.Trim();
            string port = txtPort.Text.Trim();
            if (ip.Length == 0) { Log("请输入 IP 地址"); return; }
            string target = port.Length > 0 ? ip + ":" + port : ip;
            int exit;
            string outText = ProcessRunner.Run(settings.HdcPath, "tconn " + target, out exit);
            Log("无线连接 " + target + " -> " + outText.Trim());
            CheckDevice();
        }

        private bool PrepareHap(out string hapPath)
        {
            hapPath = txtHap.Text.Trim();
            if (hapPath.Length == 0 || !File.Exists(hapPath))
            {
                Log("请先选择 HAP 文件");
                return false;
            }
            if (currentInfo == null) LoadHap(hapPath);
            return true;
        }

        private bool SignHap(string inPath, string outPath)
        {
            if (!File.Exists(settings.JavaPath)) { Log("找不到 Java: " + settings.JavaPath); return false; }
            if (!File.Exists(settings.HapSignTool)) { Log("找不到 hap-sign-tool.jar: " + settings.HapSignTool); return false; }
            if (!File.Exists(txtKeystore.Text)) { Log("找不到密钥库: " + txtKeystore.Text); return false; }
            if (!File.Exists(txtCert.Text)) { Log("找不到证书: " + txtCert.Text); return false; }
            if (!File.Exists(txtProfile.Text)) { Log("找不到 Profile: " + txtProfile.Text); return false; }

            StringBuilder args = new StringBuilder();
            args.Append(" -jar \"").Append(settings.HapSignTool).Append("\" sign-app");
            args.Append(" -mode localSign");
            args.Append(" -keyAlias \"").Append(txtAlias.Text).Append("\"");
            args.Append(" -keyPwd \"").Append(txtPwd.Text).Append("\"");
            args.Append(" -appCertFile \"").Append(txtCert.Text).Append("\"");
            args.Append(" -profileFile \"").Append(txtProfile.Text).Append("\"");
            args.Append(" -inFile \"").Append(inPath).Append("\"");
            args.Append(" -signAlg SHA256withECDSA");
            args.Append(" -keystoreFile \"").Append(txtKeystore.Text).Append("\"");
            args.Append(" -keystorePwd \"").Append(txtPwd.Text).Append("\"");
            args.Append(" -outFile \"").Append(outPath).Append("\"");
            args.Append(" -compatibleVersion ").Append(currentInfo != null ? currentInfo.MinApi : 20);
            args.Append(" -signCode 1");

            Log("开始签名...");
            int exit;
            string outText = ProcessRunner.Run(settings.JavaPath, args.ToString(), out exit);
            Log(outText.Trim());
            bool ok = File.Exists(outPath) && new FileInfo(outPath).Length > 10000;
            Log(ok ? "签名成功" : "签名失败 (exit=" + exit + ")");
            return ok;
        }

        private void SignAndInstall(bool launch)
        {
            string hap;
            if (!PrepareHap(out hap)) return;
            if (currentInfo == null) { Log("无法识别包名，中止"); return; }
            if (chkAutoProfile.Checked)
            {
                string existing = FindProfile(currentInfo.Bundle);
                if (existing == null)
                {
                    Log("该包名没有 Profile，尝试自动生成...");
                    string genResult = AutoCreateProfile();
                    if (genResult.Length > 0)
                    {
                        Log("自动生成失败: " + genResult);
                        return;
                    }
                    existing = FindProfile(currentInfo.Bundle);
                    if (existing != null) txtProfile.Text = existing;
                }
            }
            if (HuaweiApi.LoggedIn)
            {
                string certId = "";
                string certErr = HuaweiApi.EnsureCert(txtStore.Text, Log, ref certId);
                if (certErr.Length == 0)
                    txtCert.Text = Path.Combine(txtStore.Text, "hmos-debug.cer");
                else
                    Log("证书同步失败: " + certErr);
            }
            string outHap = Path.Combine(Path.GetTempPath(), currentInfo.Bundle + "_signed.hap");
            if (!SignHap(hap, outHap)) return;
            InstallWithRetry(outHap, launch, hap);
        }

        private void HuaweiLogin()
        {
            if (loggingIn)
            {
                Log("正在登录中，请稍候...");
                return;
            }
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? ".";
            if (File.Exists(Path.Combine(exeDir, "Microsoft.Web.WebView2.Core.dll")))
            {
                HuaweiApi.DebugLog("=== 华为账号登录开始 ===");
                using (LoginForm f = new LoginForm())
                {
                    f.LogCallback = Log;
                    if (f.ShowDialog(this) == DialogResult.OK)
                    {
                        SetAccountUi("登录成功");
                    }
                    else
                    {
                        SetAccountUi(f.ErrorMessage ?? "登录已取消或失败");
                    }
                }
                return;
            }
            loggingIn = true;
            lblAccount.Text = "登录中...";
            lblAccount.ForeColor = Color.FromArgb(240, 173, 78);
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                try
                {
                    string result = HuaweiApi.Login(Log);
                    SetAccountUi(result);
                }
                catch (Exception ex)
                {
                    SetAccountUi("登录异常: " + ex.Message);
                }
                finally
                {
                    loggingIn = false;
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void SetAccountUi(string result)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action<string>(SetAccountUi), result);
                    return;
                }
                Log(result);
                lblAccount.Text = HuaweiApi.LoggedIn ? "已登录 " + HuaweiApi.UserId : "未登录";
                lblAccount.ForeColor = HuaweiApi.LoggedIn ? Color.FromArgb(47, 191, 113) : Color.FromArgb(229, 72, 77);
            }
            catch { }
        }

        private void ManualLogin()
        {
            if (loggingIn)
            {
                Log("正在登录中，请稍候...");
                return;
            }
            string token = txtTempToken.Text.Trim();
            if (token.Length == 0)
            {
                Log("请先把浏览器地址栏里的 tempToken 复制到输入框");
                return;
            }
            loggingIn = true;
            lblAccount.Text = "登录中...";
            lblAccount.ForeColor = Color.FromArgb(240, 173, 78);
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                try
                {
                    SetAccountUi(HuaweiApi.ExchangeTempToken(token, Log));
                }
                catch (Exception ex)
                {
                    SetAccountUi("登录异常: " + ex.Message);
                }
                finally
                {
                    loggingIn = false;
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void ImportSaved()
        {
            if (loggingIn)
            {
                Log("正在登录中，请稍候...");
                return;
            }
            HuaweiApi.LoadAccount();
            if (!HuaweiApi.LoggedIn)
            {
                SetAccountUi("未找到已保存的账号（userInfo.json）");
                return;
            }
            lblAccount.Text = "登录中...";
            lblAccount.ForeColor = Color.FromArgb(240, 173, 78);
            System.Threading.Thread t = new System.Threading.Thread(() =>
            {
                try
                {
                    SetAccountUi(HuaweiApi.Refresh(Log));
                }
                catch (Exception ex)
                {
                    SetAccountUi("导入异常: " + ex.Message);
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        private void GenProfile()
        {
            if (currentInfo == null || currentInfo.Bundle.Length == 0)
            {
                Log("请先选择 HAP 文件");
                return;
            }
            string result = AutoCreateProfile();
            if (result.Length == 0)
            {
                Log("Profile 就绪: " + txtProfile.Text);
            }
            else
            {
                Log("生成失败: " + result);
            }
        }

        private string AutoCreateProfile()
        {
            if (!HuaweiApi.LoggedIn) return "未登录华为账号，请先点“华为账号登录”";
            Cursor = Cursors.WaitCursor;
            try
            {
                Log("开始自动生成 Profile ...");
                if (HuaweiApi.HasDebugCertOnAccount(Log) && !File.Exists(Path.Combine(txtStore.Text, "hmos.p12")))
                    return "账号已有调试证书，但 store 目录缺少配套密钥 hmos.p12；请把原签名材料（hmos.p12、hmos-debug.cer、com_*.p7b）放入 store 目录后重试";
                string keyErr = EnsureLocalKeypair();
                if (keyErr.Length > 0) return keyErr;
                string certId = "";
                string certErr = HuaweiApi.EnsureCert(txtStore.Text, Log, ref certId);
                if (certErr.Length > 0) return certErr;
                txtCert.Text = Path.Combine(txtStore.Text, "hmos-debug.cer");
                string udid = GetUdid();
                if (udid.Length == 0 || udid.StartsWith("获取")) return "获取设备 UDID 失败: " + udid;
                List<string> deviceIds = HuaweiApi.EnsureDevice(udid, Log);
                List<string> acl = new List<string>();
                foreach (string p in currentInfo.Permissions)
                {
                    if (aclBlacklist.Contains(p)) continue;
                    foreach (string a in HuaweiApi.DefaultAcl)
                    {
                        if (p == a) { acl.Add(p); break; }
                    }
                }
                Log("Profile 权限清单: " + (acl.Count > 0 ? string.Join(", ", acl) : "(无受限权限)"));
                string err = HuaweiApi.CreateProfile(currentInfo.Bundle, acl, deviceIds, certId, txtStore.Text, Log);
                if (err.Length > 0) return err;
                string profile = FindProfile(currentInfo.Bundle);
                if (profile != null) txtProfile.Text = profile;
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private string EnsureLocalKeypair()
        {
            string storeDir = txtStore.Text;
            string csrPath = Path.Combine(storeDir, "hmos.csr");
            string p12Path = Path.Combine(storeDir, "hmos.p12");
            if (File.Exists(csrPath) && File.Exists(p12Path))
            {
                if (txtKeystore.Text != p12Path) txtKeystore.Text = p12Path;
                return "";
            }
            if (!File.Exists(settings.JavaPath) || !File.Exists(settings.HapSignTool))
                return "缺少 Java/hap-sign-tool，无法生成密钥";
            Log("未找到本地密钥，正在自动生成（首次使用只需登录即可）...");
            int code;
            string kArgs = string.Format(
                " -jar \"{0}\" generate-keypair -keyAlias hmos -keyPwd hmos123 -keyAlg ECC -keySize NIST-P-256 -keystoreFile \"{1}\" -keystorePwd hmos123",
                settings.HapSignTool, p12Path);
            ProcessRunner.Run(settings.JavaPath, kArgs, out code);
            if (!File.Exists(p12Path)) return "密钥生成失败";
            txtPwd.Text = "hmos123";
            string cArgs = string.Format(
                " -jar \"{0}\" generate-csr -keyAlias hmos -keyPwd hmos123 -subject \"C=CN,O=HUAWEI,OU=HUAWEI IDE,CN=hmos\" -signAlg SHA256withECDSA -keystoreFile \"{1}\" -keystorePwd hmos123 -outFile \"{2}\"",
                settings.HapSignTool, p12Path, csrPath);
            ProcessRunner.Run(settings.JavaPath, cArgs, out code);
            if (!File.Exists(csrPath)) return "CSR 生成失败";
            txtKeystore.Text = p12Path;
            Log("已自动生成密钥与 CSR");
            return "";
        }

        private string GetUdid()
        {
            int exit;
            string outText = ProcessRunner.Run(settings.HdcPath, "shell bm get --udid", out exit);
            string[] parts = outText.Split(':');
            return parts.Length > 1 ? parts[parts.Length - 1].Trim() : outText.Trim();
        }

        private void InstallOnly()
        {
            string hap;
            if (!PrepareHap(out hap)) return;
            InstallWithRetry(hap, true, null);
        }

        private void InstallWithRetry(string hap, bool launch, string originalHap)
        {
            if (!File.Exists(settings.HdcPath)) { Log("找不到 hdc: " + settings.HdcPath); return; }
            Log("开始安装: " + hap);
            int exit;
            string outText = ProcessRunner.Run(settings.HdcPath, "install -r \"" + hap + "\"", out exit);
            Log(outText.Trim());
            if (outText.IndexOf("successfully", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Log("安装成功！");
                if (launch) LaunchApp();
                return;
            }
            Match m = Regex.Match(outText, "PermissionName:\\s*([A-Za-z0-9_.]+)");
            if (m.Success && originalHap != null && currentInfo != null)
            {
                string perm = m.Groups[1].Value;
                Log("权限授予失败: " + perm + "，正在从安装包中剔除该权限后重试...");
                aclBlacklist.Add(perm);
                string repacked = RepackWithoutPermission(originalHap, perm);
                if (repacked != null)
                {
                    string outHap2 = Path.Combine(Path.GetTempPath(), currentInfo.Bundle + "_signed_retry.hap");
                    if (SignHap(repacked, outHap2))
                    {
                        Log("重新安装...");
                        int exit2;
                        string outText2 = ProcessRunner.Run(settings.HdcPath, "install -r \"" + outHap2 + "\"", out exit2);
                        Log(outText2.Trim());
                        if (outText2.IndexOf("successfully", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Log("安装成功！");
                            if (launch) LaunchApp();
                            return;
                        }
                    }
                }
            }
            Log("安装失败，请检查设备连接/解锁状态");
        }

        private string RepackWithoutPermission(string hapPath, string perm)
        {
            string tmpDir = null;
            try
            {
                tmpDir = Path.Combine(Path.GetTempPath(), "hapfix_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmpDir);
                using (ZipArchive src = ZipFile.OpenRead(hapPath))
                {
                    foreach (ZipArchiveEntry e in src.Entries)
                    {
                        if (e.FullName.StartsWith("META-INF/")) continue;
                        string target = Path.Combine(tmpDir, e.FullName.Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        using (Stream es = e.Open())
                        using (FileStream fs = File.Create(target))
                            es.CopyTo(fs);
                    }
                }
                string mjPath = Path.Combine(tmpDir, "module.json");
                if (!File.Exists(mjPath)) return null;
                string json = File.ReadAllText(mjPath, Encoding.UTF8);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                Dictionary<string, object> root = ser.Deserialize<Dictionary<string, object>>(json);
                if (root != null && root.ContainsKey("module"))
                {
                    Dictionary<string, object> module = root["module"] as Dictionary<string, object>;
                    if (module != null && module.ContainsKey("requestPermissions"))
                    {
                        ArrayList perms = module["requestPermissions"] as ArrayList;
                        ArrayList filtered = new ArrayList();
                        if (perms != null)
                        {
                            foreach (object o in perms)
                            {
                                Dictionary<string, object> d = o as Dictionary<string, object>;
                                if (d != null && d.ContainsKey("name") &&
                                    string.Equals((string)d["name"], perm, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                filtered.Add(o);
                            }
                        }
                        module["requestPermissions"] = filtered;
                        File.WriteAllText(mjPath, ser.Serialize(root), Encoding.UTF8);
                        Log("已从 module.json 移除权限: " + perm);
                    }
                }
                string outHap = tmpDir + ".hap";
                using (ZipArchive dest = ZipFile.Open(outHap, ZipArchiveMode.Create))
                {
                    foreach (string file in Directory.GetFiles(tmpDir, "*", SearchOption.AllDirectories))
                    {
                        string rel = file.Substring(tmpDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                        dest.CreateEntryFromFile(file, rel, CompressionLevel.NoCompression);
                    }
                }
                return outHap;
            }
            catch (Exception ex)
            {
                Log("剔除权限失败: " + ex.Message);
                return null;
            }
        }

        private void Uninstall()
        {
            if (currentInfo == null || currentInfo.Bundle.Length == 0)
            {
                Log("请先选择 HAP 文件以获取包名");
                return;
            }
            Log("卸载: " + currentInfo.Bundle);
            int exit;
            string outText = ProcessRunner.Run(settings.HdcPath, "uninstall " + currentInfo.Bundle, out exit);
            Log(outText.Trim());
        }

        private void LaunchApp()
        {
            if (currentInfo == null || currentInfo.Bundle.Length == 0)
            {
                Log("请先选择 HAP 文件以获取包名");
                return;
            }
            int exit;
            string outText = ProcessRunner.Run(settings.HdcPath,
                "shell aa start -a " + currentInfo.MainAbility + " -b " + currentInfo.Bundle, out exit);
            Log(outText.Trim());
        }

        private void SaveCfg()
        {
            settings.StoreDir = txtStore.Text;
            settings.Keystore = txtKeystore.Text;
            settings.Cert = txtCert.Text;
            settings.Profile = txtProfile.Text;
            settings.KeystorePwd = txtPwd.Text;
            settings.KeyAlias = txtAlias.Text;
            settings.AutoProfile = chkAutoProfile.Checked;
            settings.LastHap = txtHap.Text;
            settings.Save();
            Log("配置已保存");
        }

        private void OpenStoreDir()
        {
            try
            {
                string dir = txtStore.Text;
                if (Directory.Exists(dir))
                {
                    Process.Start("explorer.exe", "\"" + dir + "\"");
                    Log("已打开 Profile 目录: " + dir);
                }
                else
                {
                    Log("Profile 目录不存在: " + dir);
                }
            }
            catch (Exception ex)
            {
                Log("打开目录失败: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveCfg();
            base.OnFormClosing(e);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // 现代 HTTPS（TLS 1.2 / 1.1 / 1.0），兼容华为接口
            ServicePointManager.SecurityProtocol =
                (SecurityProtocolType)3072 | (SecurityProtocolType)768 | (SecurityProtocolType)192;
            ServicePointManager.Expect100Continue = false;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
