// ============================================================
//  3DS SD Manager - Online.cs
//  GitHub releases, downloads, extracting ZIP files
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SdManager3ds
{
    internal static class Github
    {
        static Github()
        {
            // TLS 1.2 (3072), TLS 1.1 (768), TLS 1.0 (192) - GitHub requires TLS 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | (SecurityProtocolType)192;
        }

        private static WebClient NewClient()
        {
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.UserAgent] = "SD-Manager-3DS";
            wc.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
            wc.Encoding = Encoding.UTF8;
            return wc;
        }

        public static ReleaseInfo Latest(string repo)
        {
            string json;
            using (WebClient wc = NewClient())
            {
                json = wc.DownloadString("https://api.github.com/repos/" + repo + "/releases/latest");
            }

            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = int.MaxValue;
            Dictionary<string, object> root = ser.Deserialize<Dictionary<string, object>>(json);

            ReleaseInfo ri = new ReleaseInfo();
            object v;
            if (root.TryGetValue("tag_name", out v) && v != null) { ri.Tag = v.ToString(); }
            if (root.TryGetValue("name", out v) && v != null) { ri.Title = v.ToString(); }
            if (root.TryGetValue("assets", out v))
            {
                System.Collections.IEnumerable list = v as System.Collections.IEnumerable;
                if (list != null)
                {
                    foreach (object o in list)
                    {
                        Dictionary<string, object> a = o as Dictionary<string, object>;
                        if (a == null) { continue; }
                        AssetInfo ai = new AssetInfo();
                        object x;
                        if (a.TryGetValue("name", out x) && x != null) { ai.Name = x.ToString(); }
                        if (a.TryGetValue("browser_download_url", out x) && x != null) { ai.Url = x.ToString(); }
                        if (a.TryGetValue("size", out x) && x != null) { ai.Size = Convert.ToInt64(x); }
                        ri.Assets.Add(ai);
                    }
                }
            }
            return ri;
        }

        public static void Download(string url, string dest, Action<int> progress)
        {
            if (File.Exists(dest)) { File.SetAttributes(dest, FileAttributes.Normal); File.Delete(dest); }
            bool done = false;
            Exception err = null;
            using (WebClient wc = NewClient())
            {
                wc.DownloadProgressChanged += delegate(object s, DownloadProgressChangedEventArgs e)
                {
                    if (progress != null) { progress(e.ProgressPercentage); }
                };
                wc.DownloadFileCompleted += delegate(object s, System.ComponentModel.AsyncCompletedEventArgs e)
                {
                    err = e.Error;
                    done = true;
                };
                wc.DownloadFileAsync(new Uri(url), dest);
                while (!done)
                {
                    Application.DoEvents();
                    Thread.Sleep(15);
                }
            }
            if (err != null)
            {
                throw new IOException("Download failed: " + err.Message, err);
            }
        }
    }

    internal static class ZipTools
    {
        private static void WriteEntry(ZipArchiveEntry e, string dest)
        {
            string dir = Path.GetDirectoryName(dest);
            if (!Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
            if (File.Exists(dest)) { File.SetAttributes(dest, FileAttributes.Normal); }
            e.ExtractToFile(dest, true);
        }

        // Extracts files with given names (from any subfolder of the ZIP) to destDir
        public static int ExtractByNames(string zipPath, string destDir, string[] names)
        {
            int n = 0;
            Directory.CreateDirectory(destDir);
            using (ZipArchive za = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry e in za.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) { continue; }
                    if (!Util.NameIn(names, e.Name)) { continue; }
                    WriteEntry(e, Path.Combine(destDir, e.Name));
                    n++;
                }
            }
            return n;
        }

        // Writes one file from the ZIP to several existing target paths
        public static int ReplaceFileFromZip(string zipPath, string fileName, List<string> targets)
        {
            int n = 0;
            using (ZipArchive za = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry e in za.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) { continue; }
                    if (!string.Equals(e.Name, fileName, StringComparison.OrdinalIgnoreCase)) { continue; }
                    foreach (string t in targets)
                    {
                        WriteEntry(e, t);
                        n++;
                    }
                    break;
                }
            }
            return n;
        }

        // Extracts a folder of the ZIP (e.g. "gm9") to destDir; skipSub = subfolders that are skipped
        public static int ExtractFolder(string zipPath, string prefix, string destDir, string[] skipSub)
        {
            int n = 0;
            string pfx = prefix.Replace('\\', '/').TrimEnd('/') + "/";
            using (ZipArchive za = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry e in za.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) { continue; }
                    string full = e.FullName.Replace('\\', '/');
                    if (!full.StartsWith(pfx, StringComparison.OrdinalIgnoreCase)) { continue; }
                    string rel = full.Substring(pfx.Length);
                    if (rel.Contains("..")) { continue; }
                    bool skip = false;
                    foreach (string s in skipSub)
                    {
                        if (rel.StartsWith(s + "/", StringComparison.OrdinalIgnoreCase)) { skip = true; }
                    }
                    if (skip) { continue; }
                    string dest = Path.GetFullPath(Path.Combine(destDir, rel.Replace('/', '\\')));
                    if (!Util.IsInside(destDir, dest)) { continue; }
                    WriteEntry(e, dest);
                    n++;
                }
            }
            return n;
        }
    }
}
