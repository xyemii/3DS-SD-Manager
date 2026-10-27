// ============================================================
//  3DS SD Manager - MainForm.Pack.cs
//  Starter pack, modules/updates, SD tools
// ============================================================

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SdManager3ds
{
    internal partial class MainForm
    {
        private const string GuideStart = "https://3ds.hacks.guide/get-started.html";
        private const string GuideNew = "https://3ds.hacks.guide/installing-boot9strap-(super-skaterhax).html";
        private const string GuideOld = "https://3ds.hacks.guide/installing-boot9strap-(safecerthax).html";
        private const string GuideMset9 = "https://3ds.hacks.guide/installing-boot9strap-(mset9).html";
        private const string SkaterSite = "https://skater.nintendohomebrew.com";

        private static readonly ModuleDef[] Modules = new ModuleDef[]
        {
            new ModuleDef("luma3ds", "Luma3DS (boot.firm)", "LumaTeam/Luma3DS", "^Luma3DS.*\\.zip$", ModKind.Luma, "boot.firm"),
            new ModuleDef("godmode9", "GodMode9 (payload + gm9 scripts)", "d0k3/GodMode9", "^GodMode9.*\\.zip$", ModKind.Gm9, "GodMode9.firm"),
            new ModuleDef("universal-updater", "Universal-Updater (.3dsx)", "Universal-Team/Universal-Updater", "^Universal-Updater\\.3dsx$", ModKind.App, "Universal-Updater.3dsx"),
            new ModuleDef("checkpoint", "Checkpoint (.3dsx)", "FlagBrew/Checkpoint", "^Checkpoint\\.3dsx$", ModKind.App, "Checkpoint.3dsx"),
            new ModuleDef("fbi", "FBI (.3dsx)", "nh-server/FBI-NH", "^FBI\\.3dsx$", ModKind.App, "FBI.3dsx"),
            new ModuleDef("ftpd", "ftpd (.3dsx)", "mtheall/ftpd", "^ftpd\\.3dsx$", ModKind.App, "ftpd.3dsx"),
            new ModuleDef("anemone3ds", "Anemone3DS (.3dsx)", "astronautlevel2/Anemone3DS", "^Anemone3DS\\.3dsx$", ModKind.App, "Anemone3DS.3dsx")
        };

        // Pack tab
        private RadioButton rbOld;
        private RadioButton rbNew;
        private Label lblModelInfo;
        private CheckBox chkClean;
        private CheckBox chkPhase1;
        private CheckBox chkPhase2;
        private Button btnPack;
        private Button btnGuide;
        private Button btnExploit;
        private TextBox txtLog;

        // Module tab
        private ListView lvMod;
        private Button btnModCheck;
        private Button btnModUpdate;
        private Label lblModInfo;

        // Tools tab
        private Label lblSdInfo;
        private Button btnFormat;
        private Button btnCapTest;
        private Button btnAdmin;

        private bool loadingModel;

        // =====================================================
        //  Build tabs
        // =====================================================

        private static Label NewWrapLabel(string text, int height)
        {
            Label l = new Label();
            l.AutoSize = false;
            l.Text = text;
            l.Height = height;
            return l;
        }

        private void BuildPackTab()
        {
            Panel pnlTop = new Panel();
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 346;

            Label intro = NewWrapLabel("This pack puts the files on your SD card that 3ds.hacks.guide lists as required for custom firmware (Luma3DS + boot9strap). "
                + "You still have to run the exploit on the 3DS itself - this app cannot do that for you. "
                + "Files are always downloaded in their latest version directly from GitHub.", 52);
            intro.SetBounds(14, 8, 860, 52);

            GroupBox gbModel = new GroupBox();
            gbModel.Text = "My model";
            gbModel.SetBounds(14, 66, 860, 110);
            rbOld = new RadioButton();
            rbOld.Text = "Old 3DS / Old 3DS XL / Old 2DS";
            rbOld.SetBounds(14, 22, 260, 22);
            rbNew = new RadioButton();
            rbNew.Text = "New 3DS / New 3DS XL / New 2DS XL";
            rbNew.SetBounds(290, 22, 300, 22);
            lblModelInfo = NewWrapLabel("Please choose your model. Tip: the New 3DS additionally has a C-Stick (small nub on the right) and ZL/ZR shoulder buttons.", 54);
            lblModelInfo.SetBounds(14, 48, 830, 54);
            gbModel.Controls.Add(rbOld);
            gbModel.Controls.Add(rbNew);
            gbModel.Controls.Add(lblModelInfo);

            GroupBox gbSteps = new GroupBox();
            gbSteps.Text = "What should go on the SD card?";
            gbSteps.SetBounds(14, 184, 860, 106);
            chkClean = new CheckBox();
            chkClean.Text = "First find unnecessary files and delete them (after confirmation): macOS/Windows junk, leftover ZIPs";
            chkClean.SetBounds(14, 22, 830, 22);
            chkClean.Checked = true;
            chkPhase1 = new CheckBox();
            chkPhase1.Text = "Phase 1 - Before the exploit: Luma3DS (boot.firm, boot.3dsx) + boot9strap (/boot9strap/)";
            chkPhase1.SetBounds(14, 47, 830, 22);
            chkPhase1.Checked = true;
            chkPhase2 = new CheckBox();
            chkPhase2.Text = "Phase 2 - Only AFTER boot9strap is installed: finalize.romfs + x_finalize_helper.firm";
            chkPhase2.SetBounds(14, 72, 830, 22);
            chkPhase2.Checked = false;
            gbSteps.Controls.Add(chkClean);
            gbSteps.Controls.Add(chkPhase1);
            gbSteps.Controls.Add(chkPhase2);

            btnPack = NewButton("Install pack to SD", 14, 300, 220, 34);
            btnPack.FlatStyle = FlatStyle.Flat;
            btnPack.BackColor = Color.SteelBlue;
            btnPack.ForeColor = Color.White;
            btnGuide = NewButton("Open guide (start page)", 244, 300, 190, 34);
            btnExploit = NewButton("Exploit page for my model", 444, 300, 220, 34);

            pnlTop.Controls.Add(intro);
            pnlTop.Controls.Add(gbModel);
            pnlTop.Controls.Add(gbSteps);
            pnlTop.Controls.Add(btnPack);
            pnlTop.Controls.Add(btnGuide);
            pnlTop.Controls.Add(btnExploit);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Dock = DockStyle.Fill;
            txtLog.BackColor = Color.White;

            tabPack.Controls.Add(txtLog);
            tabPack.Controls.Add(pnlTop);

            rbOld.CheckedChanged += delegate { OnModelChanged(); };
            rbNew.CheckedChanged += delegate { OnModelChanged(); };
            btnPack.Click += delegate { Safe(InstallPack); };
            btnGuide.Click += delegate { Safe(delegate { OpenUrl(GuideStart); }); };
            btnExploit.Click += delegate { Safe(OpenExploitPage); };
        }

        private void BuildModTab()
        {
            lvMod = new ListView();
            lvMod.View = View.Details;
            lvMod.CheckBoxes = true;
            lvMod.FullRowSelect = true;
            lvMod.GridLines = true;
            lvMod.HideSelection = false;
            lvMod.Columns.Add("Module", 280);
            lvMod.Columns.Add("Installed", 140);
            lvMod.Columns.Add("Latest version", 140);
            lvMod.Columns.Add("Status", 440);
            lvMod.Dock = DockStyle.Fill;

            lblModInfo = NewWrapLabel("Checks the modules found on the SD card against the latest GitHub releases. "
                + "The app only remembers versions it installed itself - everything else shows \"unknown\". "
                + "Apps installed as CIA in the HOME Menu are best updated on the 3DS itself with Universal-Updater.", 58);
            lblModInfo.Dock = DockStyle.Top;
            lblModInfo.Padding = new Padding(10, 6, 10, 0);

            Panel pnlButtons = new Panel();
            pnlButtons.Dock = DockStyle.Bottom;
            pnlButtons.Height = 50;
            btnModCheck = NewButton("Check for updates", 12, 8, 190, 34);
            btnModUpdate = NewButton("Update selected", 212, 8, 210, 34);
            pnlButtons.Controls.Add(btnModCheck);
            pnlButtons.Controls.Add(btnModUpdate);

            tabMod.Controls.Add(lvMod);
            tabMod.Controls.Add(pnlButtons);
            tabMod.Controls.Add(lblModInfo);

            btnModCheck.Click += delegate { Safe(CheckModules); };
            btnModUpdate.Click += delegate { Safe(UpdateModules); };
        }

        private void BuildToolsTab()
        {
            lblSdInfo = NewWrapLabel("No drive selected.", 150);
            lblSdInfo.Dock = DockStyle.Top;
            lblSdInfo.Padding = new Padding(10, 8, 10, 0);

            Panel pnlButtons = new Panel();
            pnlButtons.Dock = DockStyle.Top;
            pnlButtons.Height = 50;
            btnFormat = NewButton("Format as FAT32 ...", 12, 8, 230, 34);
            btnCapTest = NewButton("Check real capacity ...", 252, 8, 230, 34);
            btnAdmin = NewButton("Restart as administrator", 492, 8, 230, 34);
            pnlButtons.Controls.Add(btnFormat);
            pnlButtons.Controls.Add(btnCapTest);
            pnlButtons.Controls.Add(btnAdmin);

            Label help = NewWrapLabel("FAT32 formatting: the 3DS can only read FAT32 (no exFAT). Windows no longer formats cards above 32 GB as FAT32 itself - "
                + "so this app has its own formatter (cards up to 32 GB are formatted by Windows itself). All data on the card is lost. "
                + "Administrator rights are required.\r\n\r\n"
                + "Capacity test: fills all free space with test data and reads it back afterwards (like H2testw). "
                + "This exposes fake cards that report more storage than they really have. Existing files stay untouched, and the test files are deleted afterwards. "
                + "On large cards this takes a long time (roughly an hour per 32 GB on slow cards). Only the free space is tested - empty the card first for a full check.\r\n\r\n"
                + "Tip: click \"Scan\" once after formatting or testing.", 200);
            help.Dock = DockStyle.Fill;
            help.Padding = new Padding(10, 8, 10, 0);

            tabTools.Controls.Add(help);
            tabTools.Controls.Add(pnlButtons);
            tabTools.Controls.Add(lblSdInfo);

            btnFormat.Click += delegate { Safe(FormatDrive); };
            btnCapTest.Click += delegate { Safe(CapacityTest); };
            btnAdmin.Click += delegate { Safe(RestartElevated); };
        }

        // =====================================================
        //  General helpers
        // =====================================================

        private void Log(string s)
        {
            txtLog.AppendText(s + "\r\n");
            Application.DoEvents();
        }

        private void OpenUrl(string url)
        {
            Process.Start(url);
        }

        private void OpenExploitPage()
        {
            if (rbNew.Checked) { OpenUrl(GuideNew); }
            else if (rbOld.Checked) { OpenUrl(GuideOld); }
            else { MessageBox.Show("Please choose your model above first.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }

        private void OnModelChanged()
        {
            if (rbNew.Checked)
            {
                lblModelInfo.Text = "New 3DS / New 2DS XL: on system version 11.15.0 or newer the guide recommends \"super-skaterhax\" "
                    + "(browser exploit; you need the ZIP for your region/version from " + SkaterSite + "). Otherwise MSET9. "
                    + "The Luma3DS/boot9strap pack is the same for both methods.";
            }
            else if (rbOld.Checked)
            {
                lblModelInfo.Text = "Old 3DS / Old 2DS: super-skaterhax does NOT work. On system version 11.14.0 or older the guide recommends \"safecerthax\", "
                    + "otherwise MSET9. You download the exploit-specific files yourself as described in the guide - the Luma3DS/boot9strap pack is the same for all methods.";
            }
            if (loadingModel) { return; }
            if (state != null && (rbNew.Checked || rbOld.Checked))
            {
                state.Set("model", rbNew.Checked ? "new" : "old");
            }
        }

        private void LoadModelFromState()
        {
            if (state == null) { return; }
            string m = state.Get("model");
            loadingModel = true;
            try
            {
                if (m == "new") { rbNew.Checked = true; }
                else if (m == "old") { rbOld.Checked = true; }
            }
            finally
            {
                loadingModel = false;
            }
        }

        private void ProgressStatus(string what, int percent)
        {
            SetStatus("Downloading " + what + " ... " + percent + " %");
        }

        private string Fetch(AssetInfo a, string tmp)
        {
            string file = Path.Combine(tmp, a.Name);
            Log("  Downloading " + a.Name + (a.Size > 0 ? " (" + Util.Fmt(a.Size) + ")" : "") + " ...");
            string nm = a.Name;
            Github.Download(a.Url, file, delegate(int p) { ProgressStatus(nm, p); });
            return file;
        }

        private static string TempDir()
        {
            string tmp = Path.Combine(Path.GetTempPath(), "SDManager3DS");
            Directory.CreateDirectory(tmp);
            return tmp;
        }

        // =====================================================
        //  Starter pack
        // =====================================================

        private void InstallPack()
        {
            if (!RequireRoot()) { return; }
            if (!rbOld.Checked && !rbNew.Checked)
            {
                MessageBox.Show("Please choose your model above first (Old or New 3DS).", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!chkClean.Checked && !chkPhase1.Checked && !chkPhase2.Checked)
            {
                MessageBox.Show("Nothing is selected.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true);
            try
            {
                txtLog.Clear();
                Log("Target: " + root);
                Log("Model: " + (rbNew.Checked ? "New 3DS / New 2DS XL" : "Old 3DS / Old 2DS"));

                if (Util.IsDriveRoot(root))
                {
                    DriveInfo di = new DriveInfo(Path.GetPathRoot(root));
                    Log("File system: " + di.DriveFormat + ", size: " + Util.Fmt(di.TotalSize) + ", free: " + Util.Fmt(di.AvailableFreeSpace));
                    if (!string.Equals(di.DriveFormat, "FAT32", StringComparison.OrdinalIgnoreCase))
                    {
                        DialogResult dr = MessageBox.Show("The card is formatted as \"" + di.DriveFormat + "\". The 3DS can only read FAT32.\n\n"
                            + "Recommendation: format it as FAT32 first under \"SD tools\".\n\nContinue anyway?",
                            "Not FAT32", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                        if (dr != DialogResult.Yes) { Log("Cancelled (card is not FAT32)."); return; }
                    }
                }
                else
                {
                    Log("Note: the target is a folder, not a whole drive - the file system is not checked.");
                }

                state.Set("model", rbNew.Checked ? "new" : "old");
                string tmp = TempDir();

                if (chkClean.Checked) { CleanStep(); }
                if (chkPhase1.Checked) { Phase1(tmp); }
                if (chkPhase2.Checked) { Phase2(tmp); }

                Log("");
                Log("=== Done ===");
                if (chkPhase1.Checked)
                {
                    Log("Next steps:");
                    if (rbNew.Checked)
                    {
                        Log(" 1. Open the exploit method for the New 3DS (button \"Exploit page for my model\").");
                        Log(" 2. For super-skaterhax: copy the ZIP for your region/system version from " + SkaterSite + " to the root of the SD card (overwrite files).");
                    }
                    else
                    {
                        Log(" 1. Open the exploit method for the Old 3DS (button \"Exploit page for my model\").");
                        Log(" 2. If your system version does not fit, the guide uses MSET9 instead: " + GuideMset9);
                    }
                    Log(" 3. Put the SD card into the 3DS and follow the guide's instructions.");
                    Log(" 4. Afterwards install \"Phase 2\" here (Finalizing Setup).");
                }
                if (chkPhase2.Checked)
                {
                    Log("Phase 2: power off the console, hold X and power on - the finalizing script then starts (guide: https://3ds.hacks.guide/finalizing-setup.html).");
                }
            }
            catch (Exception ex)
            {
                Log("");
                Log("ERROR: " + ex.Message);
                MessageBox.Show(ex.Message, "Pack installation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                RefreshModuleRows();
            }
        }

        private void CleanStep()
        {
            Log("");
            Log("Looking for unnecessary files ...");
            List<Entry> junk = ScanJunk();
            if (junk.Count == 0)
            {
                Log("  No unnecessary files found.");
                return;
            }
            long total = 0;
            StringBuilder lines = new StringBuilder();
            int shown = 0;
            foreach (Entry en in junk)
            {
                total += en.Size;
                if (shown < 12)
                {
                    lines.Append("  - ").Append(en.Path.Substring(root.TrimEnd('\\').Length)).Append("\n");
                    shown++;
                }
            }
            string more = junk.Count > 12 ? "  ... and " + (junk.Count - 12) + " more\n" : "";
            DialogResult dr = MessageBox.Show(junk.Count + " unnecessary files/folders found (" + Util.Fmt(total) + "):\n\n"
                + lines.ToString() + more + "\nDelete them permanently now?",
                "Delete unnecessary files", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (dr != DialogResult.Yes)
            {
                Log("  Cleanup skipped.");
                return;
            }
            int ok = 0;
            int bad = 0;
            foreach (Entry en in junk)
            {
                try
                {
                    if (!Util.IsInside(root, en.Path)) { bad++; continue; }
                    Util.ForceDelete(en.Path, en.IsDir);
                    ok++;
                }
                catch (Exception)
                {
                    bad++;
                }
            }
            Log("  Deleted: " + ok + (bad > 0 ? ", failed: " + bad : ""));
        }

        private void Phase1(string tmp)
        {
            Log("");
            Log("Phase 1: Luma3DS ...");
            ReleaseInfo luma = Github.Latest("LumaTeam/Luma3DS");
            AssetInfo la = luma.Find("^Luma3DS.*\\.zip$");
            if (la == null) { throw new IOException("No ZIP file was found in the latest Luma3DS release."); }
            string lzip = Fetch(la, tmp);
            int n = ZipTools.ExtractByNames(lzip, root, new string[] { "boot.firm", "boot.3dsx" });
            if (n == 0) { throw new IOException("boot.firm was not found in the Luma3DS ZIP."); }
            state.Set("luma3ds", luma.Tag);
            Log("  Luma3DS " + luma.Tag + " installed (" + n + " files in the root).");

            Log("Phase 1: boot9strap ...");
            ReleaseInfo b9s = Github.Latest("SciresM/boot9strap");
            AssetInfo ba = b9s.Find("^boot9strap-\\d+(\\.\\d+)*\\.zip$");
            if (ba == null) { throw new IOException("No matching ZIP file was found in the latest boot9strap release."); }
            string bzip = Fetch(ba, tmp);
            int m = ZipTools.ExtractByNames(bzip, Path.Combine(root, "boot9strap"), new string[] { "boot9strap.firm", "boot9strap.firm.sha" });
            if (m == 0) { throw new IOException("boot9strap.firm was not found in the ZIP."); }
            Log("  boot9strap " + b9s.Tag + " copied to /boot9strap/ (" + m + " files).");
        }

        private void Phase2(string tmp)
        {
            Log("");
            Log("Phase 2: finalizing files ...");
            string romfs = Path.Combine(tmp, "finalize.romfs");
            string helper = Path.Combine(tmp, "x_finalize_helper.firm");
            Log("  Downloading finalize.romfs ...");
            Github.Download("https://github.com/hacks-guide/finalize/releases/latest/download/finalize.romfs", romfs,
                delegate(int p) { ProgressStatus("finalize.romfs", p); });
            Log("  Downloading x_finalize_helper.firm ...");
            Github.Download("https://github.com/hacks-guide/finalize/releases/latest/download/x_finalize_helper.firm", helper,
                delegate(int p) { ProgressStatus("x_finalize_helper.firm", p); });

            string payloads = Path.Combine(root, "luma\\payloads");
            Directory.CreateDirectory(payloads);
            File.Copy(romfs, Path.Combine(root, "finalize.romfs"), true);
            File.Copy(helper, Path.Combine(payloads, "x_finalize_helper.firm"), true);
            Log("  finalize.romfs -> root, x_finalize_helper.firm -> /luma/payloads/");
        }

        // =====================================================
        //  Modules and updates
        // =====================================================

        private List<string> FindModuleFiles(ModuleDef d)
        {
            List<string> res = new List<string>();
            if (root == null) { return res; }
            if (d.Kind == ModKind.Luma)
            {
                string p = Path.Combine(root, "boot.firm");
                if (File.Exists(p)) { res.Add(p); }
            }
            else if (d.Kind == ModKind.Gm9)
            {
                foreach (FileSystemInfo e in Util.SafeList(Path.Combine(root, "luma\\payloads")))
                {
                    FileInfo f = e as FileInfo;
                    if (f == null) { continue; }
                    if (f.Name.IndexOf("GodMode9", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        string.Equals(f.Extension, ".firm", StringComparison.OrdinalIgnoreCase))
                    {
                        res.Add(f.FullName);
                    }
                }
            }
            else
            {
                foreach (FileInfo f in Util.FindFiles(Path.Combine(root, "3ds"), ".3dsx"))
                {
                    if (string.Equals(f.Name, d.FileName, StringComparison.OrdinalIgnoreCase)) { res.Add(f.FullName); }
                }
            }
            return res;
        }

        private void RefreshModuleRows()
        {
            if (lvMod == null) { return; }
            lvMod.Items.Clear();
            foreach (ModuleDef d in Modules)
            {
                ListViewItem li = new ListViewItem(d.Name);
                bool present = (root != null) && FindModuleFiles(d).Count > 0;
                string tag = (state != null) ? state.Get(d.Key) : null;
                li.SubItems.Add(present ? (tag ?? "unknown") : "-");
                li.SubItems.Add("");
                li.SubItems.Add(present ? "Not checked yet - click \"Check for updates\"." : "Not found on the SD card.");
                ModRow mr = new ModRow();
                mr.Def = d;
                li.Tag = mr;
                lvMod.Items.Add(li);
            }
        }

        private void UpdateRowStatus(ListViewItem li)
        {
            ModRow mr = (ModRow)li.Tag;
            bool present = FindModuleFiles(mr.Def).Count > 0;
            string tag = (state != null) ? state.Get(mr.Def.Key) : null;
            li.SubItems[1].Text = present ? (tag ?? "unknown") : "-";
            if (!present)
            {
                li.SubItems[3].Text = "Not found on the SD card.";
                li.Checked = false;
                return;
            }
            if (mr.Release == null)
            {
                li.SubItems[3].Text = "Not checked yet.";
                return;
            }
            li.SubItems[2].Text = mr.Release.Tag;
            if (tag == null)
            {
                li.SubItems[3].Text = "Installed version unknown - update to " + mr.Release.Tag + " possible.";
                li.Checked = false;
            }
            else if (string.Equals(tag, mr.Release.Tag, StringComparison.OrdinalIgnoreCase))
            {
                li.SubItems[3].Text = "Up to date.";
                li.Checked = false;
            }
            else
            {
                li.SubItems[3].Text = "Update available: " + tag + " -> " + mr.Release.Tag;
                li.Checked = true;
            }
        }

        private void CheckModules()
        {
            if (!RequireRoot()) { return; }
            SetBusy(true);
            try
            {
                foreach (ListViewItem li in lvMod.Items)
                {
                    ModRow mr = (ModRow)li.Tag;
                    if (FindModuleFiles(mr.Def).Count == 0)
                    {
                        UpdateRowStatus(li);
                        continue;
                    }
                    SetStatus("Checking " + mr.Def.Name + " ...");
                    try
                    {
                        mr.Release = Github.Latest(mr.Def.Repo);
                        UpdateRowStatus(li);
                    }
                    catch (Exception ex)
                    {
                        mr.Release = null;
                        li.SubItems[3].Text = "Check failed: " + Util.ShortMsg(ex);
                    }
                }
                SetStatus("Module check finished.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void InstallModule(ModuleDef d, ReleaseInfo r, string tmp)
        {
            AssetInfo a = r.Find(d.AssetRegex);
            if (a == null) { throw new IOException("In release " + r.Tag + " no matching file was found."); }
            string nm = a.Name;
            string file = Path.Combine(tmp, a.Name);
            Github.Download(a.Url, file, delegate(int p) { ProgressStatus(nm, p); });

            if (d.Kind == ModKind.Luma)
            {
                int n = ZipTools.ExtractByNames(file, root, new string[] { "boot.firm", "boot.3dsx" });
                if (n == 0) { throw new IOException("boot.firm not found in the ZIP."); }
            }
            else if (d.Kind == ModKind.Gm9)
            {
                List<string> targets = FindModuleFiles(d);
                if (targets.Count == 0) { throw new IOException("No GodMode9 file found in /luma/payloads."); }
                int n = ZipTools.ReplaceFileFromZip(file, "GodMode9.firm", targets);
                if (n == 0) { throw new IOException("GodMode9.firm not found in the ZIP."); }
                ZipTools.ExtractFolder(file, "gm9", Path.Combine(root, "gm9"), new string[] { "support", "out", "backups" });
            }
            else
            {
                List<string> targets = FindModuleFiles(d);
                if (targets.Count == 0) { throw new IOException("App not found on the SD card."); }
                foreach (string t in targets)
                {
                    File.SetAttributes(t, FileAttributes.Normal);
                    File.Copy(file, t, true);
                }
            }
        }

        private void UpdateModules()
        {
            if (!RequireRoot()) { return; }
            List<ListViewItem> sel = new List<ListViewItem>();
            foreach (ListViewItem li in lvMod.CheckedItems) { sel.Add(li); }
            if (sel.Count == 0)
            {
                MessageBox.Show("Nothing is checked. Click \"Check for updates\" first and select modules.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (ListViewItem li in sel)
            {
                if (((ModRow)li.Tag).Release == null)
                {
                    MessageBox.Show("Please click \"Check for updates\" first.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            if (MessageBox.Show(sel.Count + " module(s) on the SD card will be replaced by the latest version. Continue?",
                "Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            int ok = 0;
            List<string> fail = new List<string>();
            SetBusy(true);
            try
            {
                string tmp = TempDir();
                foreach (ListViewItem li in sel)
                {
                    ModRow mr = (ModRow)li.Tag;
                    SetStatus("Updating " + mr.Def.Name + " ...");
                    try
                    {
                        InstallModule(mr.Def, mr.Release, tmp);
                        state.Set(mr.Def.Key, mr.Release.Tag);
                        UpdateRowStatus(li);
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        fail.Add(mr.Def.Name + ": " + Util.ShortMsg(ex));
                        li.SubItems[3].Text = "Error: " + Util.ShortMsg(ex);
                    }
                }
            }
            finally
            {
                SetBusy(false);
            }
            string msg = ok + " of " + sel.Count + " module(s) updated.";
            if (fail.Count > 0) { msg += "\n\nErrors:\n" + string.Join("\n", fail.ToArray()); }
            SetStatus("Module update finished.");
            MessageBox.Show(msg, "Result", MessageBoxButtons.OK, fail.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        // =====================================================
        //  SD tools
        // =====================================================

        private void UpdateSdInfo()
        {
            if (lblSdInfo == null) { return; }
            StringBuilder sb = new StringBuilder();
            sb.Append("Administrator rights: ").Append(Util.IsAdmin() ? "yes" : "no (required for formatting)").Append("\r\n\r\n");
            if (root == null)
            {
                sb.Append("No drive selected.");
            }
            else
            {
                sb.Append("Target: ").Append(root).Append("\r\n");
                try
                {
                    if (Util.IsDriveRoot(root))
                    {
                        DriveInfo di = new DriveInfo(Path.GetPathRoot(root));
                        sb.Append("Label: ").Append(di.VolumeLabel).Append("\r\n");
                        sb.Append("Type: ").Append(di.DriveType.ToString()).Append("\r\n");
                        sb.Append("File system: ").Append(di.DriveFormat);
                        if (!string.Equals(di.DriveFormat, "FAT32", StringComparison.OrdinalIgnoreCase)) { sb.Append("   <-- the 3DS needs FAT32!"); }
                        sb.Append("\r\n");
                        sb.Append("Reported size: ").Append(Util.Fmt(di.TotalSize)).Append("   free: ").Append(Util.Fmt(di.AvailableFreeSpace)).Append("\r\n");
                    }
                    else
                    {
                        sb.Append("This is a folder, not a whole drive - formatting and the capacity test are not possible.\r\n");
                    }
                }
                catch (Exception ex)
                {
                    sb.Append("Info not readable: ").Append(Util.ShortMsg(ex));
                }
            }
            lblSdInfo.Text = sb.ToString();
        }

        private void RestartElevated()
        {
            if (Util.IsAdmin())
            {
                MessageBox.Show("The app is already running with administrator rights.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(Application.ExecutablePath);
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                Process.Start(psi);
                Application.Exit();
            }
            catch (Win32Exception)
            {
                MessageBox.Show("The restart as administrator was cancelled.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RunFormatVolume(char letter, string label)
        {
            string args = "-NoProfile -ExecutionPolicy Bypass -Command \"Format-Volume -DriveLetter " + letter
                + " -FileSystem FAT32 -NewFileSystemLabel '" + label + "' -Confirm:$false -ErrorAction Stop\"";
            ProcessStartInfo psi = new ProcessStartInfo("powershell.exe", args);
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            using (Process p = Process.Start(psi))
            {
                while (!p.HasExited)
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
                string err = p.StandardError.ReadToEnd();
                if (p.ExitCode != 0)
                {
                    if (err == null || err.Trim().Length == 0) { err = "Format-Volume failed (exit code " + p.ExitCode + ")."; }
                    throw new IOException(err.Trim());
                }
            }
        }

        private void FormatDrive()
        {
            if (!RequireRoot()) { return; }
            if (!Util.IsDriveRoot(root))
            {
                MessageBox.Show("Formatting only works for a whole drive (not for a folder). Please select the SD card as a drive at the top.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string rootPath = Path.GetPathRoot(root);
            char letter = char.ToUpperInvariant(rootPath[0]);
            if (string.Equals(rootPath, Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("This is the Windows system drive - it will not be formatted.", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            DriveInfo di = new DriveInfo(rootPath);
            if (di.TotalSize > 1024L * 1024 * 1024 * 1024)
            {
                MessageBox.Show("This drive is larger than 1 TB and does not look like an SD card - aborting.", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            string extra = "";
            if (di.DriveType != DriveType.Removable)
            {
                extra = "Windows does NOT report this drive as removable media (type: " + di.DriveType + "). Double-check that it really is your SD card!";
            }
            string driveText = rootPath + "  \"" + di.VolumeLabel + "\"  " + di.DriveFormat + "  " + Util.Fmt(di.TotalSize);

            string label;
            using (FormatDialog dlg = new FormatDialog(driveText, letter, extra))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) { return; }
                label = FatFormatter.CleanLabel(dlg.VolumeLabel);
            }

            if (!Util.IsAdmin())
            {
                if (MessageBox.Show("Formatting requires administrator rights. Restart the app as administrator now?\n\n(Afterwards select the drive again and click Format.)",
                    "Administrator rights", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    RestartElevated();
                }
                return;
            }

            long totalSize = di.TotalSize;
            SetBusy(true);
            try
            {
                if (totalSize <= 34359738368L)
                {
                    SetStatus("Formatting " + rootPath + " with Windows (FAT32) ...");
                    RunFormatVolume(letter, label);
                }
                else
                {
                    SetStatus("Formatting " + rootPath + " with the app's own FAT32 formatter ...");
                    FatFormatter.Format(letter, label, delegate(string s) { SetStatus(s); });
                }
                Thread.Sleep(2000);
                DriveInfo d2 = new DriveInfo(rootPath);
                string fmt2 = d2.IsReady ? d2.DriveFormat : "not ready";
                if (string.Equals(fmt2, "FAT32", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Done: " + rootPath + " is now FAT32 (" + Util.Fmt(d2.TotalSize) + ", label \"" + label + "\").", "Format", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("The process finished, but Windows now reports: \"" + fmt2 + "\".\n\nPlease eject and reinsert the card and check again. If it stays like this, format it with another tool (e.g. guiformat).",
                        "Format", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                SetBusy(false);
            }
            SetRoot(root);
            Scan();
        }

        private void CapacityTest()
        {
            if (!RequireRoot()) { return; }
            string rootPath = Path.GetPathRoot(root);
            if (string.Equals(rootPath, Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("This is the Windows system drive - the test is meant for SD cards only.", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            DriveInfo di = new DriveInfo(rootPath);
            long free = di.AvailableFreeSpace;
            double minutes = (free / 1048576.0) / 15.0 / 60.0 * 2.0;
            DialogResult dr = MessageBox.Show("The test will write " + Util.Fmt(free) + " of free space on " + rootPath + " full of test data and read it back.\n\n"
                + "Estimated duration: about " + Math.Max(1, (int)Math.Round(minutes)) + " minutes (depending on card speed).\n"
                + "Your existing files stay untouched; the test files are deleted afterwards.\n\nStart the test?",
                "Capacity test", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dr != DialogResult.Yes) { return; }

            ProgressForm pf = new ProgressForm("SD card test: " + rootPath);
            CapResult r;
            SetBusy(true);
            pf.Show(this);
            try
            {
                r = CapTest.Run(rootPath,
                    delegate(string t, int p) { pf.Report(t, p); },
                    delegate { Application.DoEvents(); return pf.Cancelled; });
            }
            finally
            {
                pf.Close();
                pf.Dispose();
                SetBusy(false);
            }

            string msg;
            MessageBoxIcon icon = MessageBoxIcon.Information;
            if (r.Cancelled)
            {
                msg = "The test was cancelled. The test files have been deleted.";
            }
            else if (r.Error != null)
            {
                msg = "The test ended with an error:\n\n" + r.Error
                    + "\n\nA write/read error in the middle of the test can also indicate a defective or fake card.";
                icon = MessageBoxIcon.Warning;
            }
            else if (r.FirstBad < 0)
            {
                msg = "PASSED.\n\nThe entire free space (" + Util.Fmt(r.BlocksWritten * CapTest.Block) + ") could be written and read back without errors.\n\n"
                    + "Write: approx. " + r.WriteMBps.ToString("N1") + " MB/s\nRead: approx. " + r.ReadMBps.ToString("N1") + " MB/s\n\n"
                    + "Note: only the free space was tested. The used part of the card was not checked.";
            }
            else
            {
                long goodBytes = r.BlocksGood * (long)CapTest.Block;
                long used = di.TotalSize - r.FreeAtStart;
                if (used < 0) { used = 0; }
                msg = "FAILED - suspected fake or defective card!\n\n"
                    + "Out of " + Util.Fmt(r.BlocksWritten * CapTest.Block) + " written, only " + Util.Fmt(goodBytes) + " could be read back correctly. "
                    + "The first error occurred at " + Util.Fmt(r.FirstBad * CapTest.Block) + ".\n\n"
                    + "Roughly estimated real usable size: " + Util.Fmt(goodBytes + used) + " (reported: " + Util.Fmt(di.TotalSize) + ").\n\n"
                    + "Do not use this card for important data (save games, backups).";
                icon = MessageBoxIcon.Error;
            }
            MessageBox.Show(msg, "Capacity test result", MessageBoxButtons.OK, icon);
            Scan();
        }
    }
}
