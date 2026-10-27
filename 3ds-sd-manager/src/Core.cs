// ============================================================
//  3DS SD Manager - Core.cs
//  Program entry point, data classes, helper functions
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SdManager3ds
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Unexpected error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal class Entry
    {
        public string Name = "";
        public string Cat = "";
        public string Author = "";
        public string Info = "";
        public string Path = "";
        public bool IsDir;
        public long Size;
        public bool Critical;
        public string Note;
    }

    internal class DriveItem
    {
        public string Text;
        public string Path;
        public int Order;
        public override string ToString() { return Text; }
    }

    internal enum ModKind { Luma, Gm9, App }

    internal class ModuleDef
    {
        public string Key;
        public string Name;
        public string Repo;
        public string AssetRegex;
        public ModKind Kind;
        public string FileName;

        public ModuleDef(string key, string name, string repo, string assetRegex, ModKind kind, string fileName)
        {
            Key = key;
            Name = name;
            Repo = repo;
            AssetRegex = assetRegex;
            Kind = kind;
            FileName = fileName;
        }
    }

    internal class AssetInfo
    {
        public string Name = "";
        public string Url = "";
        public long Size;
    }

    internal class ReleaseInfo
    {
        public string Tag = "";
        public string Title = "";
        public List<AssetInfo> Assets = new List<AssetInfo>();

        public AssetInfo Find(string regex)
        {
            Regex r = new Regex(regex, RegexOptions.IgnoreCase);
            foreach (AssetInfo a in Assets)
            {
                if (r.IsMatch(a.Name)) { return a; }
            }
            return null;
        }
    }

    internal class ModRow
    {
        public ModuleDef Def;
        public ReleaseInfo Release;
    }

    // Remembers on the SD card which versions this app installed
    internal class SdState
    {
        private string path;
        private Dictionary<string, string> data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public SdState(string root)
        {
            path = System.IO.Path.Combine(root, ".sdmanager3ds.txt");
            Load();
        }

        private void Load()
        {
            data.Clear();
            try
            {
                if (File.Exists(path))
                {
                    foreach (string line in File.ReadAllLines(path, Encoding.UTF8))
                    {
                        int i = line.IndexOf('=');
                        if (i > 0) { data[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim(); }
                    }
                }
            }
            catch (Exception) { }
        }

        public string Get(string key)
        {
            string v;
            if (data.TryGetValue(key, out v)) { return v; }
            return null;
        }

        public void Set(string key, string value)
        {
            data[key] = value;
            Save();
        }

        private void Save()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (KeyValuePair<string, string> kv in data)
                {
                    sb.Append(kv.Key).Append('=').Append(kv.Value).Append("\r\n");
                }
                if (File.Exists(path)) { File.SetAttributes(path, FileAttributes.Normal); }
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                File.SetAttributes(path, FileAttributes.Hidden);
            }
            catch (Exception) { }
        }
    }

    internal static class Util
    {
        public static string Fmt(long b)
        {
            if (b >= 1073741824L) { return string.Format("{0:N2} GB", b / 1073741824.0); }
            if (b >= 1048576L) { return string.Format("{0:N1} MB", b / 1048576.0); }
            if (b >= 1024L) { return string.Format("{0:N0} KB", b / 1024.0); }
            return b.ToString() + " B";
        }

        public static bool NameIn(string[] arr, string name)
        {
            foreach (string s in arr)
            {
                if (string.Equals(s, name, StringComparison.OrdinalIgnoreCase)) { return true; }
            }
            return false;
        }

        public static FileSystemInfo[] SafeList(string dir)
        {
            try
            {
                return new DirectoryInfo(dir).GetFileSystemInfos();
            }
            catch (Exception)
            {
                return new FileSystemInfo[0];
            }
        }

        public static long DirSize(string path)
        {
            long total = 0;
            Stack<string> st = new Stack<string>();
            st.Push(path);
            while (st.Count > 0)
            {
                string d = st.Pop();
                try
                {
                    DirectoryInfo di = new DirectoryInfo(d);
                    foreach (FileInfo f in di.GetFiles()) { total += f.Length; }
                    foreach (DirectoryInfo s in di.GetDirectories())
                    {
                        if ((s.Attributes & FileAttributes.ReparsePoint) == 0) { st.Push(s.FullName); }
                    }
                }
                catch (Exception) { }
            }
            return total;
        }

        public static List<FileInfo> FindFiles(string dir, string ext)
        {
            List<FileInfo> res = new List<FileInfo>();
            Stack<string> st = new Stack<string>();
            st.Push(dir);
            while (st.Count > 0)
            {
                string cur = st.Pop();
                foreach (FileSystemInfo e in SafeList(cur))
                {
                    FileInfo f = e as FileInfo;
                    if (f != null)
                    {
                        if (string.Equals(f.Extension, ext, StringComparison.OrdinalIgnoreCase)) { res.Add(f); }
                    }
                    else if ((e.Attributes & FileAttributes.ReparsePoint) == 0)
                    {
                        st.Push(e.FullName);
                    }
                }
            }
            return res;
        }

        public static string CutNull(string s)
        {
            int z = s.IndexOf('\0');
            if (z >= 0) { s = s.Substring(0, z); }
            return s.Trim();
        }

        // true if p lies strictly inside root (not equal to root)
        public static bool IsInside(string root, string p)
        {
            string rootFull = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            string full = Path.GetFullPath(p);
            return full.Length > rootFull.Length && full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsDriveRoot(string path)
        {
            string full = Path.GetFullPath(path);
            string pr = Path.GetPathRoot(full);
            return string.Equals(full.TrimEnd('\\'), pr.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

        public static void ForceDelete(string path, bool isDir)
        {
            if (isDir)
            {
                if (!Directory.Exists(path)) { return; }
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo f in di.GetFiles("*", SearchOption.AllDirectories)) { f.Attributes = FileAttributes.Normal; }
                foreach (DirectoryInfo d in di.GetDirectories("*", SearchOption.AllDirectories)) { d.Attributes = FileAttributes.Normal; }
                di.Attributes = FileAttributes.Normal;
                di.Delete(true);
            }
            else
            {
                if (!File.Exists(path)) { return; }
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        public static bool IsAdmin()
        {
            try
            {
                WindowsPrincipal wp = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return wp.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string ShortMsg(Exception ex)
        {
            string m = ex.Message;
            if (m == null) { return ex.GetType().Name; }
            m = m.Replace("\r", " ").Replace("\n", " ");
            if (m.Length > 160) { m = m.Substring(0, 157) + "..."; }
            return m;
        }
    }
}
