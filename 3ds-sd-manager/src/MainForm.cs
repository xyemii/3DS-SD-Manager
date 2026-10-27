// ============================================================
//  3DS SD Manager - MainForm.cs
//  Main window, drives, scan (homebrew / junk), deletion
// ============================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SdManager3ds
{
    internal partial class MainForm : Form
    {
        private const string CatApp = "Homebrew apps (.3dsx)";
        private const string CatCia = "CIA installers (.cia)";
        private const string CatPlg = "Luma plugins (.3gx)";
        private const string CatPay = "Payloads / boot files";
        private const string CatData = "Homebrew data and configurations";
        private const string CatCore = "Core CFW / boot (CRITICAL)";

        private static readonly string[] CatOrder = new string[] { CatApp, CatCia, CatPlg, CatPay, CatData, CatCore };

        private static readonly string[] JunkNames = new string[] {
            ".DS_Store", ".AppleDouble", ".AppleDB", ".AppleDesktop", ".Spotlight-V100", ".Trashes",
            ".fseventsd", ".TemporaryItems", ".DocumentRevisions-V100", ".apdisk", ".VolumeIcon.icns",
            "__MACOSX", ".localized", "Thumbs.db", "ehthumbs.db", "desktop.ini"
        };
        private static readonly string[] ArchiveExts = new string[] { ".zip", ".7z", ".rar" };
        private static readonly string[] SkipDirs = new string[] { "System Volume Information", "$RECYCLE.BIN" };
        private static readonly string[] SaveDirNames = new string[] { "saves", "extdata", "backups", "backup", "sdmc" };

        private string root;
        private SdState state;

        private ComboBox cmb;
        private Label lblInfo;
        private Label lblStatus;
        private TabControl tabs;
        private TabPage tabPack;
        private TabPage tabMod;
        private TabPage tabHb;
        private TabPage tabJunk;
        private TabPage tabTools;
        private ListView lvHb;
        private ListView lvJunk;
        private Panel pnlListButtons;
        private Button btnScan;
        private Button btnDelete;
        private Button btnAll;
        private Button btnNone;
        private Button btnRefresh;
        private Button btnBrowse;

        public MainForm()
        {
            BuildUi();
            this.Shown += delegate { Safe(OnShown); };
        }

        // ---------------- User interface ----------------

        private static ListView NewList()
        {
            ListView lv = new ListView();
            lv.View = View.Details;
            lv.CheckBoxes = true;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.ShowGroups = true;
            lv.HideSelection = false;
            lv.Dock = DockStyle.Fill;
            return lv;
        }

        private static Label NewHint(string text)
        {
            Label l = new Label();
            l.AutoSize = false;
            l.Text = text;
            l.Dock = DockStyle.Top;
            l.Height = 26;
            l.TextAlign = ContentAlignment.MiddleLeft;
            return l;
        }

        private static Button NewButton(string text, int x, int y, int w, int h)
        {
            Button b = new Button();
            b.Text = text;
            b.SetBounds(x, y, w, h);
            return b;
        }

        private void BuildUi()
        {
            this.Text = "3DS SD Manager";
            this.ClientSize = new Size(1080, 720);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9f);
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception) { }

            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabPack = new TabPage("1) Starter pack");
            tabMod = new TabPage("2) Modules and updates");
            tabHb = new TabPage("Homebrew");
            tabJunk = new TabPage("Junk (macOS/Windows)");
            tabTools = new TabPage("SD tools");
            tabs.TabPages.Add(tabPack);
            tabs.TabPages.Add(tabMod);
            tabs.TabPages.Add(tabHb);
            tabs.TabPages.Add(tabJunk);
            tabs.TabPages.Add(tabTools);

            lvHb = NewList();
            lvHb.Columns.Add("Name", 280);
            lvHb.Columns.Add("Description", 230);
            lvHb.Columns.Add("Author", 130);
            lvHb.Columns.Add("Size", 80);
            lvHb.Columns.Add("Path", 300);
            tabHb.Controls.Add(lvHb);
            tabHb.Controls.Add(NewHint("Tick everything that should be deleted. Entries marked in red are risky (core CFW, save/NAND backups) and are never preselected."));

            lvJunk = NewList();
            lvJunk.Columns.Add("Name", 260);
            lvJunk.Columns.Add("Size", 90);
            lvJunk.Columns.Add("Path", 640);
            tabJunk.Controls.Add(lvJunk);
            tabJunk.Controls.Add(NewHint("Leftovers created by macOS/Windows (._filename, .DS_Store, .Spotlight-V100, Thumbs.db, leftover ZIPs in the root ...). All are unnecessary and ticked."));

            BuildPackTab();
            BuildModTab();
            BuildToolsTab();

            // Bottom bar
            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 90;

            pnlListButtons = new Panel();
            pnlListButtons.Dock = DockStyle.Top;
            pnlListButtons.Height = 48;
            btnAll = NewButton("Select all (except risky)", 12, 10, 210, 32);
            btnNone = NewButton("None", 230, 10, 90, 32);
            btnDelete = NewButton("Delete selected", 340, 10, 200, 32);
            btnDelete.FlatStyle = FlatStyle.Flat;
            btnDelete.BackColor = Color.Firebrick;
            btnDelete.ForeColor = Color.White;
            pnlListButtons.Controls.Add(btnAll);
            pnlListButtons.Controls.Add(btnNone);
            pnlListButtons.Controls.Add(btnDelete);
            pnlListButtons.Visible = false;

            lblStatus = new Label();
            lblStatus.AutoSize = false;
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Padding = new Padding(12, 0, 0, 0);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Text = "Ready.";

            bottom.Controls.Add(lblStatus);
            bottom.Controls.Add(pnlListButtons);

            lblInfo = new Label();
            lblInfo.AutoSize = false;
            lblInfo.Dock = DockStyle.Top;
            lblInfo.Height = 26;
            lblInfo.Padding = new Padding(10, 0, 0, 0);
            lblInfo.TextAlign = ContentAlignment.MiddleLeft;
            lblInfo.Text = "No drive selected.";

            FlowLayoutPanel top = new FlowLayoutPanel();
            top.Dock = DockStyle.Top;
            top.Height = 44;
            top.Padding = new Padding(8, 8, 8, 0);
            top.WrapContents = false;

            Label lblDrive = new Label();
            lblDrive.Text = "SD card / folder:";
            lblDrive.AutoSize = true;
            lblDrive.Margin = new Padding(3, 7, 6, 0);

            cmb = new ComboBox();
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.Width = 450;

            btnRefresh = new Button();
            btnRefresh.Text = "Reload drives";
            btnRefresh.Width = 140;
            btnBrowse = new Button();
            btnBrowse.Text = "Choose folder ...";
            btnBrowse.Width = 120;
            btnScan = new Button();
            btnScan.Text = "Scan";
            btnScan.Width = 90;

            top.Controls.Add(lblDrive);
            top.Controls.Add(cmb);
            top.Controls.Add(btnRefresh);
            top.Controls.Add(btnBrowse);
            top.Controls.Add(btnScan);

            // Order matters: Fill first, then Bottom/Top
            this.Controls.Add(tabs);
            this.Controls.Add(bottom);
            this.Controls.Add(lblInfo);
            this.Controls.Add(top);

            cmb.SelectionChangeCommitted += delegate
            {
                Safe(delegate
                {
                    DriveItem di = cmb.SelectedItem as DriveItem;
                    if (di != null)
                    {
                        SetRoot(di.Path);
                        Scan();
                    }
                });
            };
            btnRefresh.Click += delegate { Safe(UpdateDrives); };
            btnBrowse.Click += delegate { Safe(BrowseFolder); };
            btnScan.Click += delegate { Safe(Scan); };
            btnAll.Click += delegate
            {
                Safe(delegate
                {
                    foreach (ListViewItem li in ActiveList().Items)
                    {
                        li.Checked = !((Entry)li.Tag).Critical;
                    }
                });
            };
            btnNone.Click += delegate
            {
                Safe(delegate
                {
                    foreach (ListViewItem li in ActiveList().Items) { li.Checked = false; }
                });
            };
            btnDelete.Click += delegate { Safe(delegate { DeleteChecked(ActiveList()); }); };
            tabs.SelectedIndexChanged += delegate
            {
                pnlListButtons.Visible = (tabs.SelectedTab == tabHb || tabs.SelectedTab == tabJunk);
                if (tabs.SelectedTab == tabTools) { Safe(UpdateSdInfo); }
            };
        }

        private ListView ActiveList()
        {
            return tabs.SelectedTab == tabJunk ? lvJunk : lvHb;
        }

        private bool busy;

        private void Safe(Action a)
        {
            if (busy) { return; }
            try
            {
                a();
            }
            catch (Exception ex)
            {
                busy = false;
                this.Cursor = Cursors.Default;
                this.Enabled = true;
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetStatus(string text)
        {
            lblStatus.Text = text;
            Application.DoEvents();
        }

        private void SetBusy(bool value)
        {
            busy = value;
            this.Cursor = value ? Cursors.WaitCursor : Cursors.Default;
            btnScan.Enabled = !value;
            btnDelete.Enabled = !value;
            btnRefresh.Enabled = !value;
            btnBrowse.Enabled = !value;
            cmb.Enabled = !value;
            Application.DoEvents();
        }

        private bool RequireRoot()
        {
            if (root == null || !Directory.Exists(root))
            {
                MessageBox.Show("Please select a drive or a folder at the top first.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }

        private void OnShown()
        {
            UpdateDrives();
            if (root != null)
            {
                string t;
                if (Hits(root, out t) >= 2) { Scan(); }
            }
            else
            {
                SetStatus("No drives found. Insert an SD card and click \"Reload drives\" or choose a folder.");
            }
        }

        private void BrowseFolder()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Choose the SD card or a folder containing the SD card contents";
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SetRoot(dlg.SelectedPath);
                    Scan();
                }
            }
        }

        // ---------------- Drives ----------------

        private static int Hits(string r, out string text)
        {
            List<string> hits = new List<string>();
            string[] names = new string[] { "3ds", "luma", "Nintendo 3DS", "gm9", "cias", "boot9strap" };
            foreach (string n in names)
            {
                if (Directory.Exists(Path.Combine(r, n))) { hits.Add(n); }
            }
            text = string.Join(", ", hits.ToArray());
            return hits.Count;
        }

        private void SetRoot(string path)
        {
            root = Path.GetFullPath(path);
            state = new SdState(root);
            string t;
            int h = Hits(root, out t);
            if (h >= 2)
            {
                lblInfo.ForeColor = Color.DarkGreen;
                lblInfo.Text = "3DS SD card detected: " + root + "   (found: " + t + ")";
            }
            else
            {
                lblInfo.ForeColor = Color.DarkOrange;
                lblInfo.Text = "Selected: " + root + "   - no typical 3DS folders found (normal for a fresh card).";
            }
            LoadModelFromState();
            UpdateSdInfo();
            RefreshModuleRows();
        }

        private void UpdateDrives()
        {
            cmb.Items.Clear();
            string sysRoot = Path.GetPathRoot(Environment.SystemDirectory);
            List<DriveItem> cands = new List<DriveItem>();
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                try
                {
                    if (!d.IsReady) { continue; }
                    if (string.Equals(d.Name, sysRoot, StringComparison.OrdinalIgnoreCase)) { continue; }
                    bool rem = (d.DriveType == DriveType.Removable);
                    DriveItem it = new DriveItem();
                    it.Text = d.Name + "  \"" + d.VolumeLabel + "\"  " + d.DriveFormat + "  " + Util.Fmt(d.TotalSize) + "  " + (rem ? "Removable" : d.DriveType.ToString());
                    it.Path = d.RootDirectory.FullName;
                    it.Order = rem ? 0 : 1;
                    cands.Add(it);
                }
                catch (Exception) { }
            }
            cands.Sort(delegate(DriveItem a, DriveItem b)
            {
                int cmpRes = a.Order.CompareTo(b.Order);
                if (cmpRes != 0) { return cmpRes; }
                return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
            });
            foreach (DriveItem c in cands) { cmb.Items.Add(c); }

            int pick = -1;
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                string t;
                if (Hits(((DriveItem)cmb.Items[i]).Path, out t) >= 2) { pick = i; break; }
            }
            if (pick < 0)
            {
                for (int i = 0; i < cmb.Items.Count; i++)
                {
                    if (((DriveItem)cmb.Items[i]).Order == 0) { pick = i; break; }
                }
            }
            if (pick >= 0)
            {
                cmb.SelectedIndex = pick;
                SetRoot(((DriveItem)cmb.Items[pick]).Path);
            }
        }

        // ---------------- Scan helpers ----------------

        private static string SaveNote(string dir)
        {
            foreach (FileSystemInfo e in Util.SafeList(dir))
            {
                if ((e.Attributes & FileAttributes.Directory) != 0 && Util.NameIn(SaveDirNames, e.Name))
                {
                    return "probably contains save game backups (subfolder \"" + e.Name + "\")";
                }
            }
            return null;
        }

        // Reads name/description/author from the SMDH of a .3dsx (if present)
        private static void ReadSmdh(string file, out string name, out string desc, out string author)
        {
            name = "";
            desc = "";
            author = "";
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    if (fs.Length < 0x2C) { return; }
                    if (Encoding.ASCII.GetString(br.ReadBytes(4)) != "3DSX") { return; }
                    ushort headerSize = br.ReadUInt16();
                    if (headerSize < 0x2C) { return; }
                    fs.Seek(0x20, SeekOrigin.Begin);
                    long smdhOff = br.ReadUInt32();
                    long smdhSize = br.ReadUInt32();
                    if (smdhOff == 0 || smdhSize < 0x2008) { return; }
                    if (smdhOff + 0x2008 > fs.Length) { return; }
                    fs.Seek(smdhOff, SeekOrigin.Begin);
                    byte[] buf = br.ReadBytes(0x2008);
                    if (buf.Length < 0x2008) { return; }
                    if (Encoding.ASCII.GetString(buf, 0, 4) != "SMDH") { return; }

                    int[] langs = new int[] { 1, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                    foreach (int lang in langs)
                    {
                        int off = 8 + lang * 0x200;
                        string shortDesc = Util.CutNull(Encoding.Unicode.GetString(buf, off, 0x80));
                        if (shortDesc.Length > 0)
                        {
                            name = shortDesc;
                            desc = Util.CutNull(Encoding.Unicode.GetString(buf, off + 0x80, 0x100));
                            author = Util.CutNull(Encoding.Unicode.GetString(buf, off + 0x180, 0x80));
                            return;
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private static Entry NewEntry(string name, string cat, string info, string author, string path, bool isDir, long size, bool critical, string note)
        {
            Entry e = new Entry();
            e.Name = name;
            e.Cat = cat;
            e.Info = info;
            e.Author = author;
            e.Path = path;
            e.IsDir = isDir;
            e.Size = size;
            e.Critical = critical;
            e.Note = note;
            return e;
        }

        private static Entry AppEntry(string file, string target, bool isDir, string note)
        {
            string name, desc, author;
            ReadSmdh(file, out name, out desc, out author);
            if (string.IsNullOrEmpty(name)) { name = Path.GetFileNameWithoutExtension(file); }
            long size;
            if (isDir) { size = Util.DirSize(target); } else { size = new FileInfo(target).Length; }
            return NewEntry(name, CatApp, desc, author, target, isDir, size, note != null, note);
        }

        private void AddKnown(List<Entry> items, string name, string display, string cat, bool isDir, bool critical, string note)
        {
            string p = Path.Combine(root, name);
            if (isDir)
            {
                if (!Directory.Exists(p)) { return; }
                items.Add(NewEntry(display, cat, "", "", p, true, Util.DirSize(p), critical, note));
            }
            else
            {
                if (!File.Exists(p)) { return; }
                items.Add(NewEntry(display, cat, "", "", p, false, new FileInfo(p).Length, critical, note));
            }
        }

        // ---------------- Homebrew scan ----------------

        private List<Entry> ScanHomebrew()
        {
            List<Entry> items = new List<Entry>();

            AddKnown(items, "luma", "Luma3DS (/luma): settings, patches, payloads, plugins", CatCore, true, true,
                "Luma3DS settings, patches and payloads will be lost");
            AddKnown(items, "boot.firm", "boot.firm (Luma3DS boot file)", CatCore, false, true,
                "without it the console only boots the stock firmware");
            AddKnown(items, "boot9strap", "boot9strap (/boot9strap)", CatCore, true, true,
                "usually just an installation leftover, but part of the CFW setup");
            AddKnown(items, "gm9", "GodMode9 data (/gm9)", CatCore, true, true,
                "may contain NAND backups and scripts - do not lose your backups!");
            AddKnown(items, "Themes", "Anemone themes (/Themes)", CatData, true, false, null);
            AddKnown(items, "Splashes", "Splash screens (/Splashes)", CatData, true, false, null);
            AddKnown(items, "cheats", "Cheats (/cheats)", CatData, true, false, null);

            foreach (FileSystemInfo e in Util.SafeList(root))
            {
                FileInfo f = e as FileInfo;
                if (f == null) { continue; }
                if (string.Equals(f.Name, "boot.firm", StringComparison.OrdinalIgnoreCase)) { continue; }
                string ext = f.Extension.ToLowerInvariant();
                if (ext == ".3dsx")
                {
                    items.Add(AppEntry(f.FullName, f.FullName, false, null));
                }
                else if (ext == ".cia")
                {
                    items.Add(NewEntry(f.Name, CatCia, "", "", f.FullName, false, f.Length, false, null));
                }
                else if (ext == ".firm" || ext == ".bin")
                {
                    items.Add(NewEntry(f.Name, CatPay, "", "", f.FullName, false, f.Length, false, null));
                }
            }

            string d3 = Path.Combine(root, "3ds");
            if (Directory.Exists(d3))
            {
                SetStatus("Looking for homebrew in /3ds ...");
                foreach (FileSystemInfo e in Util.SafeList(d3))
                {
                    Application.DoEvents();
                    FileInfo fi = e as FileInfo;
                    if (fi != null)
                    {
                        if (string.Equals(fi.Extension, ".3dsx", StringComparison.OrdinalIgnoreCase))
                        {
                            items.Add(AppEntry(fi.FullName, fi.FullName, false, null));
                        }
                        continue;
                    }
                    if (e.Name.StartsWith(".", StringComparison.Ordinal)) { continue; }

                    List<FileInfo> apps = Util.FindFiles(e.FullName, ".3dsx");
                    string note = SaveNote(e.FullName);
                    if (apps.Count == 1)
                    {
                        items.Add(AppEntry(apps[0].FullName, e.FullName, true, note));
                    }
                    else if (apps.Count > 1)
                    {
                        foreach (FileInfo a in apps)
                        {
                            items.Add(AppEntry(a.FullName, a.FullName, false, null));
                        }
                    }
                    else
                    {
                        items.Add(NewEntry(e.Name + "  (folder without .3dsx)", CatData, "", "", e.FullName, true,
                            Util.DirSize(e.FullName), note != null, note));
                    }
                }
            }

            SetStatus("Looking for CIA files ...");
            foreach (FileInfo f in Util.FindFiles(Path.Combine(root, "cias"), ".cia"))
            {
                items.Add(NewEntry(f.Name, CatCia, "", "", f.FullName, false, f.Length, false, null));
            }

            foreach (FileSystemInfo e in Util.SafeList(Path.Combine(root, "luma\\payloads")))
            {
                FileInfo f = e as FileInfo;
                if (f == null) { continue; }
                if (string.Equals(f.Extension, ".firm", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(f.Extension, ".bin", StringComparison.OrdinalIgnoreCase))
                {
                    items.Add(NewEntry(f.Name + "  (Luma payload)", CatPay, "", "", f.FullName, false, f.Length, false, null));
                }
            }

            foreach (FileInfo f in Util.FindFiles(Path.Combine(root, "luma\\plugins"), ".3gx"))
            {
                string parent = f.Directory != null ? f.Directory.Name : "";
                string info = string.Equals(parent, "plugins", StringComparison.OrdinalIgnoreCase) ? "" : "Title ID folder: " + parent;
                items.Add(NewEntry(f.Name, CatPlg, info, "", f.FullName, false, f.Length, false, null));
            }

            return items;
        }

        // ---------------- Junk scan (macOS / Windows) ----------------

        private List<Entry> ScanJunk()
        {
            List<Entry> found = new List<Entry>();
            Stack<string> st = new Stack<string>();
            st.Push(root);
            int count = 0;
            while (st.Count > 0)
            {
                string dir = st.Pop();
                bool isRootDir = string.Equals(dir.TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
                count++;
                if (count % 40 == 0)
                {
                    string shortDir = dir;
                    if (shortDir.Length > 90) { shortDir = "..." + shortDir.Substring(shortDir.Length - 87); }
                    SetStatus("Looking for unnecessary files ... " + shortDir);
                }
                foreach (FileSystemInfo e in Util.SafeList(dir))
                {
                    bool isDir = (e.Attributes & FileAttributes.Directory) != 0;
                    bool isLink = (e.Attributes & FileAttributes.ReparsePoint) != 0;
                    string name = e.Name;
                    bool isDouble = name.StartsWith("._", StringComparison.Ordinal);
                    bool isArchive = (!isDir) && isRootDir && Util.NameIn(ArchiveExts, Path.GetExtension(name));
                    if (isDouble || isArchive || Util.NameIn(JunkNames, name))
                    {
                        string typ;
                        if (isDouble) { typ = "AppleDouble file (._)"; }
                        else if (string.Equals(name, ".DS_Store", StringComparison.OrdinalIgnoreCase)) { typ = "Finder metadata (.DS_Store)"; }
                        else if (isArchive) { typ = "Archive in root (download leftover)"; }
                        else if (string.Equals(name, "Thumbs.db", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(name, "ehthumbs.db", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase)) { typ = "Windows system file"; }
                        else if (isDir) { typ = "macOS system folder"; }
                        else { typ = "macOS system file"; }

                        long size = 0;
                        if (isDir) { size = Util.DirSize(e.FullName); }
                        else
                        {
                            FileInfo fi = e as FileInfo;
                            if (fi != null) { size = fi.Length; }
                        }
                        found.Add(NewEntry(name, typ, "", "", e.FullName, isDir, size, false, null));
                        continue;
                    }
                    if (isDir && !isLink && !Util.NameIn(SkipDirs, name)) { st.Push(e.FullName); }
                }
            }
            return found;
        }

        // ---------------- Fill lists ----------------

        private void FillHomebrew(List<Entry> items)
        {
            items.Sort(delegate(Entry a, Entry b)
            {
                int c = Array.IndexOf(CatOrder, a.Cat).CompareTo(Array.IndexOf(CatOrder, b.Cat));
                if (c != 0) { return c; }
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            string rootTrim = root.TrimEnd('\\');
            Dictionary<string, ListViewGroup> groups = new Dictionary<string, ListViewGroup>();
            lvHb.BeginUpdate();
            lvHb.Items.Clear();
            lvHb.Groups.Clear();
            foreach (Entry it in items)
            {
                ListViewGroup g;
                if (!groups.TryGetValue(it.Cat, out g))
                {
                    g = new ListViewGroup(it.Cat, it.Cat);
                    lvHb.Groups.Add(g);
                    groups[it.Cat] = g;
                }
                ListViewItem li = new ListViewItem(it.Name);
                li.SubItems.Add(it.Info ?? "");
                li.SubItems.Add(it.Author ?? "");
                li.SubItems.Add(Util.Fmt(it.Size));
                li.SubItems.Add(it.Path.Substring(rootTrim.Length));
                li.Group = g;
                li.Tag = it;
                if (it.Critical) { li.ForeColor = Color.Firebrick; }
                lvHb.Items.Add(li);
            }
            lvHb.EndUpdate();
            tabHb.Text = "Homebrew (" + items.Count + ")";
        }

        private void FillJunk(List<Entry> items)
        {
            items.Sort(delegate(Entry a, Entry b)
            {
                int c = string.Compare(a.Cat, b.Cat, StringComparison.OrdinalIgnoreCase);
                if (c != 0) { return c; }
                return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
            });

            string rootTrim = root.TrimEnd('\\');
            Dictionary<string, ListViewGroup> groups = new Dictionary<string, ListViewGroup>();
            lvJunk.BeginUpdate();
            lvJunk.Items.Clear();
            lvJunk.Groups.Clear();
            foreach (Entry it in items)
            {
                ListViewGroup g;
                if (!groups.TryGetValue(it.Cat, out g))
                {
                    g = new ListViewGroup(it.Cat, it.Cat);
                    lvJunk.Groups.Add(g);
                    groups[it.Cat] = g;
                }
                ListViewItem li = new ListViewItem(it.Name);
                li.SubItems.Add(Util.Fmt(it.Size));
                li.SubItems.Add(it.Path.Substring(rootTrim.Length));
                li.Group = g;
                li.Tag = it;
                li.Checked = true;
                lvJunk.Items.Add(li);
            }
            lvJunk.EndUpdate();
            tabJunk.Text = "Junk (" + items.Count + ")";
        }

        private void Scan()
        {
            if (!RequireRoot()) { return; }
            SetBusy(true);
            try
            {
                SetStatus("Looking for homebrew ...");
                List<Entry> hb = ScanHomebrew();
                FillHomebrew(hb);
                List<Entry> junk = ScanJunk();
                long junkSize = 0;
                foreach (Entry m in junk) { junkSize += m.Size; }
                FillJunk(junk);
                RefreshModuleRows();
                SetStatus("Done: " + hb.Count + " homebrew entries, " + junk.Count + " unnecessary files (" + Util.Fmt(junkSize) + ").");
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ---------------- Deletion ----------------

        private void DeleteChecked(ListView lv)
        {
            List<ListViewItem> sel = new List<ListViewItem>();
            foreach (ListViewItem li in lv.CheckedItems) { sel.Add(li); }
            if (sel.Count == 0)
            {
                MessageBox.Show("Nothing is ticked.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            long total = 0;
            List<string> risky = new List<string>();
            StringBuilder lines = new StringBuilder();
            int shown = 0;
            foreach (ListViewItem li in sel)
            {
                Entry en = (Entry)li.Tag;
                total += en.Size;
                if (en.Critical)
                {
                    risky.Add("  - " + en.Name + (string.IsNullOrEmpty(en.Note) ? "" : ": " + en.Note));
                }
                if (shown < 15)
                {
                    lines.Append("  - ").Append(li.Text).Append("\n");
                    shown++;
                }
            }
            string more = sel.Count > 15 ? "  ... and " + (sel.Count - 15) + " more\n" : "";

            string msg = "The following " + sel.Count + " entries (" + Util.Fmt(total) + ") will be PERMANENTLY deleted (no recycle bin!):\n\n"
                + lines.ToString() + more + "\nContinue?";
            if (MessageBox.Show(msg, "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            if (risky.Count > 0)
            {
                string msg2 = "WARNING - risky entries selected:\n\n" + string.Join("\n", risky.ToArray())
                    + "\n\nThis can break your custom firmware or irrecoverably delete backups (save games, NAND).\n\nReally delete?";
                if (MessageBox.Show(msg2, "Risky entries", MessageBoxButtons.YesNo, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    return;
                }
            }

            int ok = 0;
            List<string> fail = new List<string>();
            SetBusy(true);
            try
            {
                int n = 0;
                foreach (ListViewItem li in sel)
                {
                    n++;
                    Entry en = (Entry)li.Tag;
                    SetStatus("Deleting (" + n + "/" + sel.Count + "): " + li.Text);
                    try
                    {
                        if (!Util.IsInside(root, en.Path)) { throw new InvalidOperationException("Path is outside the selected drive - skipped."); }
                        Util.ForceDelete(en.Path, en.IsDir);
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        fail.Add(en.Path + " -> " + ex.Message);
                    }
                }
            }
            finally
            {
                SetBusy(false);
            }

            string res = "Deleted: " + ok + " of " + sel.Count;
            if (fail.Count > 0)
            {
                int take = Math.Min(8, fail.Count);
                res += "\n\nFailed (" + fail.Count + "):\n" + string.Join("\n", fail.GetRange(0, take).ToArray());
                MessageBox.Show(res, "Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show(res, "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            Scan();
        }
    }
}
