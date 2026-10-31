using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace AmdVramCleaner;

public sealed partial class MainForm : Form
{
    static readonly Color Bg = Color.FromArgb(30, 30, 30), PanelBg = Color.FromArgb(43, 43, 43), Fg = Color.FromArgb(230, 230, 230),
        Dim = Color.FromArgb(150, 150, 150), Accent = Color.FromArgb(0, 150, 220), UsedColor = Color.FromArgb(220, 60, 50), FreeColor = Color.FromArgb(70, 200, 90);
    static readonly string[] ModifierNames = { "CTRL", "CTRL + SHIFT", "CTRL + ALT", "ALT + SHIFT", "WIN + SHIFT" };
    static readonly uint[] ModifierFlags = { Native.MOD_CONTROL, Native.MOD_CONTROL | Native.MOD_SHIFT, Native.MOD_CONTROL | Native.MOD_ALT, Native.MOD_ALT | Native.MOD_SHIFT, Native.MOD_WIN | Native.MOD_SHIFT };
    const int HotkeyId = 0xA1D;

    readonly Settings S = Settings.Load();
    readonly GpuMonitor Mon = new();
    readonly NotifyIcon Tray = new();
    readonly System.Windows.Forms.Timer Tick = new() { Interval = 1500 };
    readonly List<(Control c, string key)> Texts = new();
    GpuSample Last = new(0, 0, 0, 0);
    DateTime LastAuto = DateTime.Now, OptimizedUntil = DateTime.MinValue;
    bool Busy, ReallyExit;

    Panel _vramPanel = null!, _sharedPanel = null!; Label _gpuLabel = null!; Button _optBtn = null!;
    ListBox _excluded = null!; ComboBox _procCombo = null!, _langCombo = null!, _modCombo = null!, _keyCombo = null!;
    TrackBar _everyBar = null!, _belowBar = null!; Label _everyLbl = null!, _belowLbl = null!;

    public MainForm()
    {
        L.Lang = S.Language;
        Text = "AMD VRAM Cleaner " + (Application.ProductVersion.Split('+')[0] is var v && v.Count(c => c == '.') >= 2 ? v[..v.LastIndexOf('.')] : Application.ProductVersion);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        BackColor = Bg; ForeColor = Fg; Font = new Font("Segoe UI", 9f);
        FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(930, 750); TopMost = S.AlwaysOnTop;
        Build();
        ApplyTexts();
        SetupTray();
        ApplyHotkey();
        ApplyPriority();
        Tick.Tick += (_, _) => Refresh_();
        Tick.Start();
        Refresh_();
        if (S.StartMinimized) { WindowState = FormWindowState.Minimized; if (S.CloseToTray) Hide(); }
    }

    // ---------- UI construction ----------
    Label Title(Control parent, string key)
    {
        var l = new Label { AutoSize = true, Location = new Point(8, 6), ForeColor = Fg, Font = new Font("Segoe UI Semibold", 8.5f) };
        Texts.Add((l, key)); parent.Controls.Add(l); return l;
    }

    Panel Panel_(int x, int y, int w, int h)
    {
        var p = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = PanelBg };
        Controls.Add(p); return p;
    }

    CheckBox Check(Control parent, int y, string key, Func<bool> get, Action<bool> set, int x = 8)
    {
        var c = new CheckBox { Location = new Point(x, y), AutoSize = true, Checked = get(), ForeColor = Fg, Cursor = Cursors.Hand };
        c.CheckedChanged += (_, _) => { set(c.Checked); S.Save(); };
        Texts.Add((c, key)); parent.Controls.Add(c); return c;
    }

    Label Lbl(Control parent, int x, int y, string key, Color? color = null)
    {
        var l = new Label { Location = new Point(x, y), AutoSize = true, ForeColor = color ?? Fg };
        if (key != "") Texts.Add((l, key));
        parent.Controls.Add(l); return l;
    }

    ComboBox Combo(Control parent, int x, int y, int w)
    {
        var c = new ComboBox { Location = new Point(x, y), Width = w, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(60, 60, 60), ForeColor = Fg };
        parent.Controls.Add(c); return c;
    }

    Button ColorButton(Control parent, int x, int y, Func<string> get, Action<string> set)
    {
        var b = new Button { Location = new Point(x, y), Size = new Size(96, 24), FlatStyle = FlatStyle.Flat, ForeColor = Fg, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };
        b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
        void Paint() { b.Text = "      " + get(); b.Invalidate(); }
        b.Paint += (_, e) => { using var br = new SolidBrush(ColorTranslator.FromHtml(get())); e.Graphics.FillRectangle(br, 5, 6, 12, 12); };
        b.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = ColorTranslator.FromHtml(get()), FullOpen = true };
            if (dlg.ShowDialog(this) == DialogResult.OK) { set(ColorTranslator.ToHtml(dlg.Color)); S.Save(); Paint(); UpdateTrayIcon(); }
        };
        Paint(); parent.Controls.Add(b); return b;
    }

    void Build()
    {
        // ---- Configuration ----
        var cfg = Panel_(12, 12, 255, 270);
        Title(cfg, "cfg");
        _langCombo = Combo(cfg, 8, 26, 160);
        _langCombo.Items.AddRange(new object[] { "Spanish (Español)", "English" });
        _langCombo.SelectedIndex = S.Language == "en" ? 1 : 0;
        _langCombo.SelectedIndexChanged += (_, _) => { S.Language = L.Lang = _langCombo.SelectedIndex == 1 ? "en" : "es"; S.Save(); ApplyTexts(); Refresh_(); };
        int y = 58;
        Check(cfg, y, "closeToTray", () => S.CloseToTray, v => S.CloseToTray = v); y += 24;
        Check(cfg, y, "closeAfter", () => S.CloseAfterOptimization, v => S.CloseAfterOptimization = v); y += 24;
        Check(cfg, y, "startup", () => S.RunOnStartup, v => { S.RunOnStartup = v; ApplyStartup(v); }); y += 24;
        Check(cfg, y, "lowPriority", () => S.RunOnLowPriority, v => { S.RunOnLowPriority = v; ApplyPriority(); }); y += 24;
        Check(cfg, y, "startMin", () => S.StartMinimized, v => S.StartMinimized = v); y += 24;
        Check(cfg, y, "showShared", () => S.ShowSharedMemory, v => { S.ShowSharedMemory = v; _sharedPanel.Visible = v; }); y += 24;
        Check(cfg, y, "notify", () => S.ShowNotifications, v => S.ShowNotifications = v); y += 24;
        Check(cfg, y, "onTop", () => S.AlwaysOnTop, v => { S.AlwaysOnTop = v; TopMost = v; });

        // ---- Areas ----
        var areas = Panel_(12, 278, 255, 182);
        Title(areas, "areas"); y = 26;
        Check(areas, y, "areaVram", () => S.AreaVramPressure, v => S.AreaVramPressure = v); y += 22;
        Check(areas, y, "areaWs", () => S.AreaTrimWorkingSets, v => S.AreaTrimWorkingSets = v); y += 22;
        Check(areas, y, "areaShader", () => S.AreaShaderCache, v => S.AreaShaderCache = v); y += 22;
        Check(areas, y, "areaStandby", () => S.AreaStandbyList, v => S.AreaStandbyList = v); y += 22;
        Check(areas, y, "areaFileCache", () => S.AreaFileCache, v => S.AreaFileCache = v); y += 22;
        Check(areas, y, "areaDwm", () => S.AreaRestartDwm, v => S.AreaRestartDwm = v); y += 22;
        Check(areas, y, "areaDriver", () => S.AreaRestartDriver, v => S.AreaRestartDriver = v);

        // ---- Hotkey ----
        var hk = Panel_(12, 468, 255, 68);
        Title(hk, "hotkey");
        var hkCheck = Check(hk, 32, "", () => S.HotkeyEnabled, v => { S.HotkeyEnabled = v; ApplyHotkey(); }); hkCheck.Text = "";
        _modCombo = Combo(hk, 30, 29, 110); _modCombo.Items.AddRange(ModifierNames); _modCombo.SelectedIndex = Math.Clamp(S.HotkeyModifiers, 0, ModifierNames.Length - 1);
        Lbl(hk, 142, 32, "").Text = "+";
        _keyCombo = Combo(hk, 158, 29, 50);
        for (char c = 'A'; c <= 'Z'; c++) _keyCombo.Items.Add(c.ToString());
        for (int i = 1; i <= 12; i++) _keyCombo.Items.Add("F" + i);
        _keyCombo.SelectedItem = S.HotkeyKey; if (_keyCombo.SelectedIndex < 0) _keyCombo.SelectedIndex = 6;
        _modCombo.SelectedIndexChanged += (_, _) => { S.HotkeyModifiers = _modCombo.SelectedIndex; S.Save(); ApplyHotkey(); };
        _keyCombo.SelectedIndexChanged += (_, _) => { S.HotkeyKey = (string)_keyCombo.SelectedItem!; S.Save(); ApplyHotkey(); };

        // ---- Tray icon ----
        var tray = Panel_(280, 12, 323, 240);
        Title(tray, "tray"); y = 28;
        Check(tray, y, "trayMiddle", () => S.TrayOptimizeOnMiddleClick, v => S.TrayOptimizeOnMiddleClick = v); y += 24;
        Check(tray, y, "trayUsage", () => S.TrayShowUsage, v => { S.TrayShowUsage = v; UpdateTrayIcon(); }); y += 24;
        Check(tray, y, "trayTransparent", () => S.TrayTransparent, v => { S.TrayTransparent = v; UpdateTrayIcon(); }); y += 28;
        Lbl(tray, 8, y + 4, "back"); ColorButton(tray, 130, y, () => S.TrayBackColor, v => S.TrayBackColor = v); y += 28;
        Lbl(tray, 8, y + 4, "text"); ColorButton(tray, 130, y, () => S.TrayTextColor, v => S.TrayTextColor = v); y += 28;
        Lbl(tray, 8, y + 4, "optimized"); ColorButton(tray, 130, y, () => S.TrayOptimizedColor, v => S.TrayOptimizedColor = v); y += 28;
        Lbl(tray, 8, y + 4, "warning"); Level(tray, 130, y, () => S.WarningLevel, v => S.WarningLevel = v); ColorButton(tray, 190, y, () => S.WarningColor, v => S.WarningColor = v); y += 28;
        Lbl(tray, 8, y + 4, "danger"); Level(tray, 130, y, () => S.DangerLevel, v => S.DangerLevel = v); ColorButton(tray, 190, y, () => S.DangerColor, v => S.DangerColor = v);

        // ---- Excluded processes ----
        var ex = Panel_(280, 260, 323, 160);
        Title(ex, "excluded");
        _excluded = new ListBox { Location = new Point(8, 26), Size = new Size(307, 92), BackColor = Color.FromArgb(35, 35, 35), ForeColor = Fg, BorderStyle = BorderStyle.None };
        _excluded.Items.AddRange(S.ExcludedProcesses.ToArray());
        _excluded.DoubleClick += (_, _) => { if (_excluded.SelectedItem is string s) { _excluded.Items.Remove(s); S.ExcludedProcesses.Remove(s); S.Save(); } };
        Texts.Add((_excluded, "removeHint"));
        ex.Controls.Add(_excluded);
        _procCombo = Combo(ex, 8, 126, 277); _procCombo.DropDownStyle = ComboBoxStyle.DropDown;
        _procCombo.DropDown += (_, _) => FillProcesses();
        var add = new Button { Location = new Point(291, 125), Size = new Size(24, 24), Text = "+", FlatStyle = FlatStyle.Flat, ForeColor = Fg, Cursor = Cursors.Hand };
        add.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
        add.Click += (_, _) =>
        {
            var name = _procCombo.Text.Trim();
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
            if (name.Length == 0 || S.ExcludedProcesses.Contains(name, StringComparer.OrdinalIgnoreCase)) return;
            S.ExcludedProcesses.Add(name); _excluded.Items.Add(name); S.Save();
        };
        ex.Controls.Add(add);

        // ---- Auto optimization ----
        var auto = Panel_(280, 428, 323, 120);
        Title(auto, "auto");
        _everyLbl = Lbl(auto, 8, 26, "");
        _everyBar = Slider(auto, 50, 0, 24, S.AutoEveryHours, v => { S.AutoEveryHours = v; LastAuto = DateTime.Now; UpdateAutoLabels(); });
        _belowLbl = Lbl(auto, 8, 76, "");
        _belowBar = Slider(auto, 100, 0, 90, S.AutoWhenFreeBelow, v => { S.AutoWhenFreeBelow = v; UpdateAutoLabels(); });
        UpdateAutoLabels();

        // ---- Memory bars ----
        _vramPanel = Panel_(12, 556, 591, 70); _vramPanel.Paint += (_, e) => PaintBar(e.Graphics, _vramPanel, Last.DedicatedUsed, Last.DedicatedTotal);
        _sharedPanel = Panel_(12, 632, 591, 70); _sharedPanel.Paint += (_, e) => PaintBar(e.Graphics, _sharedPanel, Last.SharedUsed, Last.SharedTotal);
        _sharedPanel.Visible = S.ShowSharedMemory;
        _gpuLabel = new Label { Location = new Point(12, 706), AutoSize = true, ForeColor = Dim, Font = new Font("Segoe UI", 8f) };
        Controls.Add(_gpuLabel);

        _optBtn = new Button { Location = new Point(12, 722), Size = new Size(591, 26), FlatStyle = FlatStyle.Flat, ForeColor = Accent, BackColor = PanelBg, Cursor = Cursors.Hand };
        _optBtn.FlatAppearance.BorderSize = 0;
        _optBtn.Click += (_, _) => Optimize(false);
        Texts.Add((_optBtn, "optimize"));
        Controls.Add(_optBtn);
        BuildExtras();
    }

    void Level(Control parent, int x, int y, Func<int> get, Action<int> set)
    {
        var n = new NumericUpDown { Location = new Point(x, y), Width = 52, Minimum = 0, Maximum = 100, Value = get(), BackColor = Color.FromArgb(60, 60, 60), ForeColor = Fg, BorderStyle = BorderStyle.FixedSingle };
        n.ValueChanged += (_, _) => { set((int)n.Value); S.Save(); UpdateTrayIcon(); };
        parent.Controls.Add(n);
    }

    TrackBar Slider(Control parent, int y, int min, int max, int value, Action<int> set)
    {
        var t = new TrackBar { Location = new Point(4, y), Width = 310, Minimum = min, Maximum = max, Value = Math.Clamp(value, min, max), TickStyle = TickStyle.None, AutoSize = false, Height = 26, BackColor = PanelBg };
        t.ValueChanged += (_, _) => { set(t.Value); S.Save(); };
        parent.Controls.Add(t); return t;
    }

    void UpdateAutoLabels()
    {
        _everyLbl.Text = S.AutoEveryHours == 0 ? L.T("disabled") : L.T("every", S.AutoEveryHours + L.T("hours"));
        _belowLbl.Text = S.AutoWhenFreeBelow == 0 ? L.T("disabled") : L.T("freeBelow", S.AutoWhenFreeBelow);
    }

    void ApplyTexts()
    {
        foreach (var (c, key) in Texts)
        {
            if (key == "") continue;
            if (c is ListBox lb) { new ToolTip().SetToolTip(lb, L.T(key)); continue; }
            c.Text = L.T(key);
        }
        UpdateAutoLabels();
        ApplyExtraTexts();
        if (Mon.Gpu == null) _gpuLabel.Text = L.T("noGpu");
        else _gpuLabel.Text = L.T("gpu", Mon.Gpu.Name, Mon.DriverVersion == "" ? "?" : Mon.DriverVersion) + (Mon.Gpu.IsAmd ? "" : "   [" + L.T("notAmd", Mon.Gpu.Name) + "]");
        Tray.ContextMenuStrip = BuildTrayMenu();
        _vramPanel.Invalidate(); _sharedPanel.Invalidate();
    }

    void FillProcesses()
    {
        _procCombo.Items.Clear();
        var names = Mon.Processes().Select(p => p.Name).Concat(Process.GetProcesses().Select(p => p.ProcessName)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToArray();
        _procCombo.Items.AddRange(names);
    }

    // ---------- Painting ----------
    void PaintBar(Graphics g, Panel p, ulong used, ulong total)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        string title = L.T(p == _vramPanel ? "vram" : "shared", GpuMonitor.Format(total));
        using var titleFont = new Font("Segoe UI Semibold", 8.5f);
        g.DrawString(title, titleFont, Brushes.Gainsboro, 8, 6);
        var rect = new Rectangle(10, 30, p.Width - 20, 6);
        double pct = total == 0 ? 0 : (double)used / total;
        int w = (int)(rect.Width * pct);
        using (var fb = new SolidBrush(FreeColor)) g.FillRectangle(fb, rect);
        using (var ub = new SolidBrush(UsedColor)) g.FillRectangle(ub, rect.X, rect.Y, w, rect.Height);
        ulong free = total > used ? total - used : 0;
        string usedTxt = $"{L.T("used")}  |  {GpuMonitor.Format(used)} ({pct * 100:0}%)";
        string freeTxt = $"{L.T("free")}  |  {GpuMonitor.Format(free)} ({(total == 0 ? 0 : 100 - pct * 100):0}%)";
        using var sq1 = new SolidBrush(UsedColor); using var sq2 = new SolidBrush(FreeColor);
        g.FillRectangle(sq1, 10, 47, 10, 10); g.DrawString(usedTxt, Font, Brushes.Gainsboro, 24, 44);
        var sz = g.MeasureString(freeTxt, Font);
        g.FillRectangle(sq2, p.Width - 24 - sz.Width, 47, 10, 10); g.DrawString(freeTxt, Font, Brushes.Gainsboro, p.Width - 10 - sz.Width, 44);
    }

    // ---------- Monitoring ----------
    void Refresh_()
    {
        if (Busy) return;
        Last = Mon.Sample();
        TrackHistory();
        _vramPanel.Invalidate(); _sharedPanel.Invalidate();
        UpdateTrayIcon();
        Tray.Text = $"VRAM {GpuMonitor.Format(Last.DedicatedUsed)} / {GpuMonitor.Format(Last.DedicatedTotal)} ({Last.DedicatedPercent:0}%)";
        if (S.AutoEveryHours > 0 && (DateTime.Now - LastAuto).TotalHours >= S.AutoEveryHours) Optimize(true);
        else if (S.AutoWhenFreeBelow > 0 && Last.DedicatedTotal > 0 && 100 - Last.DedicatedPercent < S.AutoWhenFreeBelow && (DateTime.Now - LastAuto).TotalMinutes >= 5) Optimize(true);
    }

    void UpdateTrayIcon()
    {
        int pct = (int)Math.Round(Last.DedicatedPercent);
        bool optimized = DateTime.Now < OptimizedUntil;
        Color state = optimized ? ColorTranslator.FromHtml(S.TrayOptimizedColor)
            : S.DangerLevel > 0 && pct >= S.DangerLevel ? ColorTranslator.FromHtml(S.DangerColor)
            : S.WarningLevel > 0 && pct >= S.WarningLevel ? ColorTranslator.FromHtml(S.WarningColor)
            : ColorTranslator.FromHtml(S.TrayBackColor);
        bool highlighted = optimized || (S.DangerLevel > 0 && pct >= S.DangerLevel) || (S.WarningLevel > 0 && pct >= S.WarningLevel);
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(S.TrayTransparent ? Color.Transparent : state);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            string txt = S.TrayShowUsage ? (pct >= 100 ? "99" : pct.ToString()) : "V";
            using var f = new Font("Segoe UI", S.TrayShowUsage ? 15f : 18f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var br = new SolidBrush(S.TrayTransparent && highlighted ? state : ColorTranslator.FromHtml(S.TrayTextColor));
            var sz = g.MeasureString(txt, f);
            g.DrawString(txt, f, br, (32 - sz.Width) / 2, (32 - sz.Height) / 2 + 1);
        }
        var h = bmp.GetHicon();
        var old = Tray.Icon;
        Tray.Icon = (Icon)Icon.FromHandle(h).Clone();
        DestroyIcon(h); old?.Dispose();
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);

    // ---------- Tray ----------
    void SetupTray()
    {
        Tray.Visible = true;
        Tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Middle && S.TrayOptimizeOnMiddleClick) Optimize(false); };
        Tray.DoubleClick += (_, _) => ShowWindow();
        Tray.ContextMenuStrip = BuildTrayMenu();
    }

    ContextMenuStrip BuildTrayMenu()
    {
        var m = new ContextMenuStrip { BackColor = PanelBg, ForeColor = Fg, RenderMode = ToolStripRenderMode.System };
        m.Items.Add(L.T("optimize"), null, (_, _) => Optimize(false));
        m.Items.Add(L.T("show"), null, (_, _) => ShowWindow());
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(L.T("exit"), null, (_, _) => { ReallyExit = true; Close(); });
        return m;
    }

    void ShowWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!ReallyExit && S.CloseToTray && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); return; }
        Native.UnregisterHotKey(Handle, HotkeyId);
        Tray.Visible = false; Tray.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized && S.CloseToTray) Hide();
    }

    // ---------- Hotkey / startup / priority ----------
    void ApplyHotkey()
    {
        Native.UnregisterHotKey(Handle, HotkeyId);
        if (!S.HotkeyEnabled) return;
        var key = (Keys)Enum.Parse(typeof(Keys), S.HotkeyKey);
        Native.RegisterHotKey(Handle, HotkeyId, ModifierFlags[Math.Clamp(S.HotkeyModifiers, 0, ModifierFlags.Length - 1)] | Native.MOD_NOREPEAT, (uint)key);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY && (int)m.WParam == HotkeyId) Optimize(false);
        base.WndProc(ref m);
    }

    void ApplyPriority() => Process.GetCurrentProcess().PriorityClass = S.RunOnLowPriority ? ProcessPriorityClass.Idle : ProcessPriorityClass.Normal;

    static void ApplyStartup(bool enable)
    {
        try
        {
            string args = enable
                ? $"/Create /F /TN \"AmdVramCleaner\" /SC ONLOGON /RL HIGHEST /TR \"\\\"{Application.ExecutablePath}\\\"\""
                : "/Delete /F /TN \"AmdVramCleaner\"";
            Process.Start(new ProcessStartInfo("schtasks", args) { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(5000);
        }
        catch { }
    }

    // ---------- Optimization ----------
    async void Optimize(bool automatic)
    {
        if (Busy) return;
        if (S.AreaRestartDriver && !automatic && MessageBox.Show(this, L.T("driverConfirm"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        Busy = true; LastAuto = DateTime.Now;
        _optBtn.Enabled = false; _optBtn.Text = L.T("optimizing");
        var opts = new CleanOptions
        {
            VramPressure = S.AreaVramPressure, TrimWorkingSets = S.AreaTrimWorkingSets, ShaderCache = S.AreaShaderCache,
            StandbyList = S.AreaStandbyList, FileCache = S.AreaFileCache, RestartDriver = S.AreaRestartDriver && !automatic,
            RestartDwm = S.AreaRestartDwm, DwmMinBytes = (ulong)S.DwmRestartMinMb << 20,
            Excluded = new HashSet<string>(S.ExcludedProcesses, StringComparer.OrdinalIgnoreCase),
        };
        CleanResult r;
        try { r = await Task.Run(() => GpuCleaner.Run(Mon, opts)); }
        finally { Busy = false; _optBtn.Enabled = true; _optBtn.Text = L.T("optimize"); }
        Last = r.After; OptimizedUntil = DateTime.Now.AddSeconds(5);
        _vramPanel.Invalidate(); _sharedPanel.Invalidate(); UpdateTrayIcon();
        Debug.WriteLine(string.Join(" | ", r.Log));
        if (r.DwmRestarted) AddLog(L.T("logDwm", GpuMonitor.Format(r.DwmBefore), GpuMonitor.Format(r.DwmAfter)));
        AddLog(L.T("logDone", GpuMonitor.Format(r.Before.DedicatedUsed), GpuMonitor.Format(r.After.DedicatedUsed), (r.FreedBytes < 0 ? "-" : "") + GpuMonitor.Format((ulong)Math.Abs(r.FreedBytes)), r.TrimmedProcesses, GpuMonitor.Format(r.ShaderBytes)));
        if (S.ShowNotifications)
        {
            string freed = (r.FreedBytes < 0 ? "-" : "") + GpuMonitor.Format((ulong)Math.Abs(r.FreedBytes));
            Tray.ShowBalloonTip(4000, L.T("doneTitle"), L.T("done", GpuMonitor.Format(r.Before.DedicatedUsed), GpuMonitor.Format(r.After.DedicatedUsed), freed, r.TrimmedProcesses, GpuMonitor.Format(r.ShaderBytes)), ToolTipIcon.Info);
        }
        if (S.CloseAfterOptimization && !automatic) { ReallyExit = true; Close(); }
    }
}
