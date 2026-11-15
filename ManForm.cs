using System.Diagnostics;
using System.Text;

namespace ModIt;

public class MainForm : Form
{
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly TextBox _log = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Dock = DockStyle.Fill, Font = new Font("Consolas", 9) };
    private string _lastProjectDir = "";
    private string _lastApk = "";

    public MainForm()
    {
        Text = "Mod it!";
        Width = 1100;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;

        _tabs.TabPages.Add(BuildDecompileTab());
        _tabs.TabPages.Add(BuildDexSourceTab());
        _tabs.TabPages.Add(BuildSourceMakerTab());
        _tabs.TabPages.Add(BuildSdkTab());
        _tabs.TabPages.Add(BuildFileSwapTab());
        _tabs.TabPages.Add(BuildReSignTab());
        _tabs.TabPages.Add(BuildResourcesTab());

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 550 };
        split.Panel1.Controls.Add(_tabs);
        split.Panel2.Controls.Add(_log);
        Controls.Add(split);
    }

    private TabPage BuildDecompileTab()
    {
        var page = new TabPage("Decompile / Repack");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };

        var btnSelect = new Button { Text = "Select APK", Width = 200 };
        var btnDecompile = new Button { Text = "Decompile", Width = 200, Enabled = false };
        var btnOpen = new Button { Text = "Open Project Folder", Width = 200, Enabled = false };
        var btnRepack = new Button { Text = "Repack APK", Width = 200, Enabled = false };
        var btnSign = new Button { Text = "Sign & Align → mod_*.apk", Width = 260, Enabled = false };
        var lblApk = new Label { Text = "(no APK selected)", AutoSize = true };

        btnSelect.Click += (_, _) =>
        {
            using var d = new OpenFileDialog { Filter = "APK files|*.apk" };
            if (d.ShowDialog() == DialogResult.OK)
            {
                _lastApk = d.FileName;
                lblApk.Text = _lastApk;
                btnDecompile.Enabled = true;
                Log($"Selected: {_lastApk}");
            }
        };

        btnDecompile.Click += async (_, _) =>
        {
            if (string.IsNullOrEmpty(_lastApk)) return;
            var outDir = Path.Combine(Path.GetDirectoryName(_lastApk)!, Path.GetFileNameWithoutExtension(_lastApk) + "_decoded");
            _lastProjectDir = outDir;
            btnDecompile.Enabled = false;
            try
            {
                Log($"Decompiling to {outDir} …");
                await ApkService.DecompileAsync(_lastApk, outDir, Log);
                Log("Decompile finished.");
                btnOpen.Enabled = btnRepack.Enabled = true;
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
            finally { btnDecompile.Enabled = true; }
        };

        btnOpen.Click += (_, _) =>
        {
            if (Directory.Exists(_lastProjectDir))
                Process.Start(new ProcessStartInfo("explorer.exe", _lastProjectDir) { UseShellExecute = true });
        };

        btnRepack.Click += async (_, _) =>
        {
            if (string.IsNullOrEmpty(_lastProjectDir)) return;
            var unsigned = Path.Combine(Path.GetDirectoryName(_lastProjectDir)!, "repacked_unsigned.apk");
            btnRepack.Enabled = false;
            try
            {
                Log("Repacking …");
                await ApkService.RepackAsync(_lastProjectDir, unsigned, Log);
                Log("Repack finished.");
                _lastApk = unsigned;
                btnSign.Enabled = true;
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
            finally { btnRepack.Enabled = true; }
        };

        btnSign.Click += async (_, _) =>
        {
            if (string.IsNullOrEmpty(_lastApk)) return;
            using var keyDialog = new OpenFileDialog { Filter = "Keystore|*.jks;*.keystore|All files|*.*" };
            if (keyDialog.ShowDialog() != DialogResult.OK) return;

            var alias = Prompt("Key alias", "Alias");
            if (string.IsNullOrEmpty(alias)) return;
            var storePass = Prompt("Keystore password", "Password", true);
            if (storePass == null) return;
            var keyPass = Prompt("Key password", "Password", true) ?? storePass;

            var aligned = Path.Combine(Path.GetDirectoryName(_lastApk)!, "aligned.apk");
            var modName = ApkService.MakeModFileName(_lastApk);
            var modPath = Path.Combine(Path.GetDirectoryName(_lastApk)!, modName);

            try
            {
                Log("Aligning …");
                await ApkService.AlignAsync(_lastApk, aligned, Log);
                Log("Signing …");
                await ApkService.SignAsync(aligned, keyDialog.FileName, alias, storePass, keyPass, modPath, Log);
                Log($"Done → {modPath}");
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
        };

        panel.Controls.AddRange(new Control[] { btnSelect, lblApk, btnDecompile, btnOpen, btnRepack, btnSign });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildDexSourceTab()
    {
        var page = new TabPage("DEX → Java Source");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };
        var btn = new Button { Text = "Select APK / DEX → Decompile with JADX", Width = 320 };
        btn.Click += async (_, _) =>
        {
            using var d = new OpenFileDialog { Filter = "APK / DEX|*.apk;*.dex" };
            if (d.ShowDialog() != DialogResult.OK) return;
            var outDir = Path.Combine(Path.GetDirectoryName(d.FileName)!, Path.GetFileNameWithoutExtension(d.FileName) + "_java");
            try
            {
                Log($"JADX decompiling to {outDir} …");
                await ApkService.DexToJavaAsync(d.FileName, outDir, Log);
                Log("Done.");
                Process.Start(new ProcessStartInfo("explorer.exe", outDir) { UseShellExecute = true });
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
        };
        panel.Controls.Add(btn);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildSourceMakerTab()
    {
        var page = new TabPage("Source Maker");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };
        var btn = new Button { Text = "Select APK → Export Gradle Project (JADX)", Width = 340 };
        btn.Click += async (_, _) =>
        {
            using var d = new OpenFileDialog { Filter = "APK files|*.apk" };
            if (d.ShowDialog() != DialogResult.OK) return;
            var outDir = Path.Combine(Path.GetDirectoryName(d.FileName)!, Path.GetFileNameWithoutExtension(d.FileName) + "_source");
            try
            {
                Log($"Exporting Gradle project to {outDir} …");
                await SourceMaker.GenerateAsync(d.FileName, outDir, Log);
                Log("Source project generated.");
                Process.Start(new ProcessStartInfo("explorer.exe", outDir) { UseShellExecute = true });
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
        };
        panel.Controls.Add(btn);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildSdkTab()
    {
        var page = new TabPage("SDK Manager");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };
        var txt = new TextBox { Width = 500, Text = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? "" };
        var btn = new Button { Text = "Validate SDK Path", Width = 200 };
        btn.Click += (_, _) =>
        {
            var (ok, msg) = SdkManager.Validate(txt.Text);
            Log((ok ? "OK: " : "ERROR: ") + msg);
        };
        panel.Controls.Add(new Label { Text = "Android SDK path:", AutoSize = true });
        panel.Controls.Add(txt);
        panel.Controls.Add(btn);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildFileSwapTab()
    {
        var page = new TabPage("File Swap");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };
        var btnApk = new Button { Text = "Select APK", Width = 200 };
        var btnFile = new Button { Text = "Select Replacement File", Width = 200 };
        var txtTarget = new TextBox { Width = 400, Text = "assets/config.xml" };
        var btnSwap = new Button { Text = "Swap", Width = 200, Enabled = false };
        string apk = "", file = "";

        btnApk.Click += (_, _) =>
        {
            using var d = new OpenFileDialog { Filter = "APK|*.apk" };
            if (d.ShowDialog() == DialogResult.OK) { apk = d.FileName; btnSwap.Enabled = !string.IsNullOrEmpty(file); }
        };
        btnFile.Click += (_, _) =>
        {
            using var d = new OpenFileDialog();
            if (d.ShowDialog() == DialogResult.OK) { file = d.FileName; btnSwap.Enabled = !string.IsNullOrEmpty(apk); }
        };
        btnSwap.Click += (_, _) =>
        {
            try
            {
                ApkService.SwapFile(apk, file, txtTarget.Text);
                Log($"Replaced {txtTarget.Text} — remember to re‑sign.");
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
        };

        panel.Controls.AddRange(new Control[] { btnApk, btnFile, new Label { Text = "Path inside APK:", AutoSize = true }, txtTarget, btnSwap });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildReSignTab()
    {
        var page = new TabPage("Re‑sign");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };
        var btn = new Button { Text = "Select APK → Sign", Width = 240 };
        btn.Click += async (_, _) =>
        {
            using var d = new OpenFileDialog { Filter = "APK|*.apk" };
            if (d.ShowDialog() != DialogResult.OK) return;

            using var ks = new OpenFileDialog { Filter = "Keystore|*.jks;*.keystore|All files|*.*" };
            if (ks.ShowDialog() != DialogResult.OK) return;

            var alias = Prompt("Key alias", "Alias");
            if (string.IsNullOrEmpty(alias)) return;
            var storePass = Prompt("Keystore password", "Password", true);
            if (storePass == null) return;
            var keyPass = Prompt("Key password", "Password", true) ?? storePass;

            var outApk = Path.Combine(Path.GetDirectoryName(d.FileName)!, ApkService.MakeModFileName(d.FileName));
            try
            {
                await ApkService.SignAsync(d.FileName, ks.FileName, alias, storePass, keyPass, outApk, Log);
                Log($"Signed → {outApk}");
            }
            catch (Exception ex) { Log("ERROR: " + ex.Message); }
        };
        panel.Controls.Add(btn);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildResourcesTab()
    {
        var page = new TabPage("Resources / Drawables");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(12), AutoScroll = true };
        var btnRes = new Button { Text = "Open res folder of last project", Width = 280, Enabled = false };
        var btnAssets = new Button { Text = "Open assets folder of last project", Width = 280, Enabled = false };
        var btnLib = new Button { Text = "Open lib folder of last project", Width = 280, Enabled = false };

        _tabs.SelectedIndexChanged += (_, _) =>
        {
            var hasProject = !string.IsNullOrEmpty(_lastProjectDir) && Directory.Exists(_lastProjectDir);
            btnRes.Enabled = btnAssets.Enabled = btnLib.Enabled = hasProject;
        };

        btnRes.Click += (_, _) => OpenSub("res");
        btnAssets.Click += (_, _) => OpenSub("assets");
        btnLib.Click += (_, _) => OpenSub("lib");

        void OpenSub(string sub)
        {
            var p = Path.Combine(_lastProjectDir, sub);
            if (Directory.Exists(p))
                Process.Start(new ProcessStartInfo("explorer.exe", p) { UseShellExecute = true });
            else
                Log($"Folder not found: {p}");
        }

        panel.Controls.AddRange(new Control[] { btnRes, btnAssets, btnLib });
        page.Controls.Add(panel);
        return page;
    }

    private void Log(string text)
    {
        if (InvokeRequired) { Invoke(() => Log(text)); return; }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }

    private static string? Prompt(string label, string title, bool password = false)
    {
        using var f = new Form { Text = title, Width = 360, Height = 140, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var lbl = new Label { Text = label, Left = 12, Top = 12, Width = 320 };
        var txt = new TextBox { Left = 12, Top = 36, Width = 320, UseSystemPasswordChar = password };
        var btn = new Button { Text = "OK", Left = 260, Top = 68, Width = 72, DialogResult = DialogResult.OK };
        f.Controls.AddRange(new Control[] { lbl, txt, btn });
        return f.ShowDialog() == DialogResult.OK ? txt.Text : null;
    }
}