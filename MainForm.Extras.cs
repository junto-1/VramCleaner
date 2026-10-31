using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace AmdVramCleaner;

// Process panel, VRAM history graph and session log (ideas from GpuClean / Tarkov vram-cleaner).
public sealed partial class MainForm
{
    const int HistorySeconds = 300;
    const ulong LeakThreshold = 256UL << 20;

    ListView _procList = null!; Panel _histPanel = null!, _procPanel = null!, _logPanel = null!; ListBox _log = null!; Label _procNote = null!; Panel _extPanel = null!; Button _extBtn = null!; Label _extInfo = null!; CheckBox _extAuto = null!; readonly ExtremeMode Extreme = new();
    readonly List<Button> _procButtons = new(); Button _exportBtn = null!;
    readonly Queue<(DateTime t, double pct)> Hist = new();
    readonly Dictionary<int, Queue<(DateTime t, ulong bytes)>> ProcHist = new();
    int _tickCount;

    void BuildExtras()
    {
        _procPanel = Panel_(616, 12, 302, 328);
        Title(_procPanel, "procs");
        _procList = new ListView
        {
            Location = new Point(8, 26), Size = new Size(286, 232), View = View.Details, FullRowSelect = true, HideSelection = false,
            BackColor = Color.FromArgb(35, 35, 35), ForeColor = Fg, BorderStyle = BorderStyle.None, MultiSelect = false, HeaderStyle = ColumnHeaderStyle.Nonclickable,
        };
        foreach (var (key, w) in new[] { ("colProc", 92), ("colPid", 42), ("colVram", 54), ("colShared", 0), ("colDelta", 48), ("colState", 48) })
        {
            var col = _procList.Columns.Add(L.T(key), w); col.Tag = key;
        }
        _procPanel.Controls.Add(_procList);
        _procNote = new Label { Location = new Point(8, 260), Size = new Size(286, 32), ForeColor = Color.FromArgb(150, 150, 150), Font = new Font(Font.FontFamily, 7f) };
        _procPanel.Controls.Add(_procNote);
        int x = 8;
        foreach (var (key, handler) in new (string, Action)[] { ("kill", () => KillSelected(false)), ("forceKill", () => KillSelected(true)), ("exclude", ExcludeSelected), ("dryRun", DryRun) })
        {
            var b = new Button { Location = new Point(x, 294), Size = new Size(68, 26), FlatStyle = FlatStyle.Flat, ForeColor = Fg, Cursor = Cursors.Hand, Tag = key };
            b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
            b.Click += (_, _) => handler();
            _procPanel.Controls.Add(b); _procButtons.Add(b); x += 73;
        }

        BuildExtreme();
        _histPanel = Panel_(616, 348, 302, 122);
        _histPanel.Paint += (_, e) => PaintHistory(e.Graphics, _histPanel);

        _logPanel = Panel_(616, 478, 302, 320);
        Title(_logPanel, "log");
        _log = new ListBox { Location = new Point(8, 26), Size = new Size(286, 206), BackColor = Color.FromArgb(35, 35, 35), ForeColor = Fg, BorderStyle = BorderStyle.None, HorizontalScrollbar = true, Font = new Font("Consolas", 8f) };
        _logPanel.Controls.Add(_log);
        _exportBtn = new Button { Location = new Point(8, 238), Size = new Size(286, 26), FlatStyle = FlatStyle.Flat, ForeColor = Fg, Cursor = Cursors.Hand };
        _exportBtn.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
        _exportBtn.Click += (_, _) => ExportLog();
        _logPanel.Controls.Add(_exportBtn);
        AddLog(L.T("logStart", Mon.Gpu?.Name ?? "-"));
    }

    void ApplyExtraTexts()
    {
        if (_procList == null) return;
        foreach (ColumnHeader c in _procList.Columns) c.Text = L.T((string)c.Tag!);
        _procNote.Text = L.T("procNote"); ApplyExtremeTexts();
        foreach (var b in _procButtons) b.Text = L.T((string)b.Tag!);
        _exportBtn.Text = L.T("export");
        _histPanel.Invalidate();
    }

    void TrackHistory()
    {
        var now = DateTime.Now;
        Hist.Enqueue((now, Last.DedicatedTotal == 0 ? Last.SharedPercent : Last.DedicatedPercent));
        while (Hist.Count > 0 && (now - Hist.Peek().t).TotalSeconds > HistorySeconds) Hist.Dequeue();
        _histPanel.Invalidate();
        if (_tickCount++ % 2 == 0) UpdateProcessList(now);
    }

    void UpdateProcessList(DateTime now)
    {
        var procs = Mon.Processes();
        int? selected = _procList.SelectedItems.Count > 0 ? (int)_procList.SelectedItems[0].Tag! : null;
        var alive = new HashSet<int>();
        _procList.BeginUpdate();
        _procList.Items.Clear();
        foreach (var p in procs)
        {
            alive.Add(p.Pid);
            ulong bytes = p.Dedicated > 0 ? p.Dedicated : p.Shared;
            if (!ProcHist.TryGetValue(p.Pid, out var q)) ProcHist[p.Pid] = q = new();
            q.Enqueue((now, bytes));
            while (q.Count > 1 && (now - q.Peek().t).TotalSeconds > HistorySeconds) q.Dequeue();
            long delta = (long)bytes - (long)q.Peek().bytes;
            bool hung = false;
            try { using var pr = Process.GetProcessById(p.Pid); hung = pr.MainWindowHandle != IntPtr.Zero && !pr.Responding; } catch { }
            string state = hung ? L.T("hung") : delta > (long)LeakThreshold ? L.T("leak") : S.ExcludedProcesses.Contains(p.Name, StringComparer.OrdinalIgnoreCase) ? "excl." : "";
            string deltaTxt = delta == 0 ? "" : (delta < 0 ? "-" : "+") + GpuMonitor.Format((ulong)Math.Abs(delta));
            var item = new ListViewItem(new[] { p.Name, p.Pid.ToString(), GpuMonitor.Format(p.Dedicated) + (GpuCleaner.IsCompositor(p.Name) ? "*" : ""), GpuMonitor.Format(p.Shared), deltaTxt, state }) { Tag = p.Pid };
            if (hung) item.ForeColor = ColorTranslator.FromHtml(S.DangerColor);
            else if (delta > (long)LeakThreshold) item.ForeColor = ColorTranslator.FromHtml(S.WarningColor);
            _procList.Items.Add(item);
            if (p.Pid == selected) item.Selected = true;
        }
        _procList.EndUpdate();
        foreach (var dead in ProcHist.Keys.Where(k => !alive.Contains(k)).ToList()) ProcHist.Remove(dead);
    }

    void PaintHistory(Graphics g, Panel p)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var titleFont = new Font("Segoe UI Semibold", 8.5f);
        g.DrawString(L.T("history"), titleFont, Brushes.Gainsboro, 8, 6);
        var r = new Rectangle(10, 28, p.Width - 20, p.Height - 38);
        using (var bg = new SolidBrush(Color.FromArgb(35, 35, 35))) g.FillRectangle(bg, r);
        using var grid = new Pen(Color.FromArgb(60, 60, 60));
        for (int i = 1; i < 4; i++) g.DrawLine(grid, r.Left, r.Top + r.Height * i / 4, r.Right, r.Top + r.Height * i / 4);
        if (Hist.Count < 2) return;
        var now = DateTime.Now;
        var pts = Hist.Select(h => new PointF(r.Right - (float)((now - h.t).TotalSeconds / HistorySeconds) * r.Width, r.Bottom - (float)(h.pct / 100.0) * r.Height)).ToArray();
        using var path = new GraphicsPath();
        path.AddLines(pts);
        path.AddLine(pts[^1], new PointF(pts[^1].X, r.Bottom)); path.AddLine(new PointF(pts[^1].X, r.Bottom), new PointF(pts[0].X, r.Bottom));
        using (var fill = new SolidBrush(Color.FromArgb(70, UsedColor))) g.FillPath(fill, path);
        using var pen = new Pen(UsedColor, 1.6f);
        g.DrawLines(pen, pts);
        g.DrawString($"{Hist.Last().pct:0}%", Font, Brushes.Gainsboro, r.Right - 34, r.Top + 2);
    }

    void AddLog(string line)
    {
        _log.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {line}");
        while (_log.Items.Count > 500) _log.Items.RemoveAt(_log.Items.Count - 1);
    }

    GpuProcessUsage? Selected()
    {
        if (_procList.SelectedItems.Count == 0) return null;
        int pid = (int)_procList.SelectedItems[0].Tag!;
        return Mon.Processes().FirstOrDefault(p => p.Pid == pid) ?? new GpuProcessUsage(pid, _procList.SelectedItems[0].Text, 0, 0);
    }

    async void KillSelected(bool force)
    {
        var p = Selected(); if (p == null) return;
        if (GpuCleaner.IsCompositor(p.Name))
        {
            if (MessageBox.Show(this, L.T("dwmConfirm"), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            var (ok2, b, a) = await Task.Run(() => GpuCleaner.RestartDwm(Mon, 0, new List<string>()));
            AddLog(ok2 ? L.T("logDwm", GpuMonitor.Format(b), GpuMonitor.Format(a)) : L.T("killFail", p.Name));
            return;
        }
        if (GpuCleaner.IsProtected(p.Name)) return;
        if (MessageBox.Show(this, L.T("killConfirm", p.Name, p.Pid), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        bool ok = GpuCleaner.KillProcess(p.Pid, force);
        if (!ok && !force && MessageBox.Show(this, L.T("killFail", p.Name) + "\n" + L.T("forceKill") + "?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            ok = GpuCleaner.KillProcess(p.Pid, true);
        AddLog(ok ? L.T("logKilled", p.Name, p.Pid) : L.T("killFail", p.Name));
        UpdateProcessList(DateTime.Now);
    }

    void ExcludeSelected()
    {
        var p = Selected(); if (p == null) return;
        if (S.ExcludedProcesses.Contains(p.Name, StringComparer.OrdinalIgnoreCase)) return;
        S.ExcludedProcesses.Add(p.Name); _excluded.Items.Add(p.Name); S.Save();
        AddLog(L.T("logExcluded", p.Name));
    }

    void DryRun()
    {
        var opts = new CleanOptions
        {
            VramPressure = S.AreaVramPressure, TrimWorkingSets = S.AreaTrimWorkingSets, ShaderCache = S.AreaShaderCache,
            StandbyList = S.AreaStandbyList, FileCache = S.AreaFileCache, RestartDriver = S.AreaRestartDriver,
            RestartDwm = S.AreaRestartDwm, DwmMinBytes = (ulong)S.DwmRestartMinMb << 20,
            Excluded = new HashSet<string>(S.ExcludedProcesses, StringComparer.OrdinalIgnoreCase),
        };
        var plan = GpuCleaner.Plan(Mon, opts);
        AddLog(L.T("dryRunTitle"));
        foreach (var l in plan) AddLog("  " + l.Trim());
        using var f = new Form { Text = L.T("dryRunTitle"), Size = new Size(640, 420), StartPosition = FormStartPosition.CenterParent, BackColor = Bg, ForeColor = Fg, ShowIcon = false, MinimizeBox = false };
        f.Controls.Add(new TextBox { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, WordWrap = false, BackColor = PanelBg, ForeColor = Fg, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9f), Text = string.Join(Environment.NewLine, plan) });
        f.ShowDialog(this);
    }

    void ExportLog()
    {
        using var dlg = new SaveFileDialog { Filter = "Text|*.txt", FileName = $"AmdVramCleaner-{DateTime.Now:yyyyMMdd-HHmm}.txt" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllLines(dlg.FileName, _log.Items.Cast<string>().Reverse());
    }
}

public sealed partial class MainForm
{
    void BuildExtreme()
    {
        _extPanel = Panel_(12, 796, 591, 56);
        Title(_extPanel, "extreme");
        _extBtn = new Button { Location = new Point(8, 20), Size = new Size(170, 24), FlatStyle = FlatStyle.Flat, ForeColor = Fg, Cursor = Cursors.Hand };
        _extBtn.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
        _extBtn.Click += (_, _) => ToggleExtreme();
        _extInfo = new Label { Location = new Point(186, 14), Size = new Size(397, 16), ForeColor = Color.FromArgb(170, 170, 170), Font = new Font(Font.FontFamily, 7.5f), AutoEllipsis = true };
        _extAuto = new CheckBox { Location = new Point(186, 16), Anchor = AnchorStyles.Top | AnchorStyles.Left, AutoSize = true, ForeColor = Fg, Checked = S.ExtremeAutoFullscreen, Font = new Font(Font.FontFamily, 7.5f) };
        _extAuto.CheckedChanged += (_, _) => { S.ExtremeAutoFullscreen = _extAuto.Checked; S.Save(); };
        _extPanel.Controls.AddRange(new Control[] { _extBtn, _extInfo, _extAuto });
        ApplyExtremeTexts();
        Shown += (_, _) => RestoreExtreme();
    }

    void ApplyExtremeTexts()
    {
        if (_extBtn == null) return;
        foreach (Control c in _extPanel.Controls) if (c is Label l && l.Tag is string k) l.Text = L.T(k);
        _extBtn.Text = L.T(Extreme.Active ? "extremeExit" : "extremeEnter");
        _extAuto.Location = new Point(186, 34); _extAuto.AutoSize = false; _extAuto.Width = 170; _extAuto.Text = L.T("extremeAuto");
        _extAuto.Visible = !Extreme.Active;
        _extInfo.Size = Extreme.Active ? new Size(397, 30) : new Size(397, 14);
        _extInfo.Text = Extreme.Active ? L.T("extremeOn", Extreme.Current!.GameName, Extreme.Current.Closed.Count) : L.T("extremeIdle");
    }

    void RestoreExtreme()
    {
        var st = ExtremeMode.LoadPending();
        if (st == null || st.Closed.Count == 0) { try { File.Delete(ExtremeMode.StateFile); } catch { } return; }
        Extreme.Restore(st);
        if (!Extreme.GameExited()) { ApplyExtremeTexts(); return; }
        bool reopen = MessageBox.Show(this, L.T("extremeRestore", st.Closed.Count), L.T("extremeRestoreTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        var log = Extreme.Exit(reopen);
        AddLog(L.T("logExtremeOff", log.Count(l => l.StartsWith("relaunched"))));
        ApplyExtremeTexts();
    }

    async void ToggleExtreme()
    {
        if (Extreme.Active) { LeaveExtreme(); return; }
        (int pid, string name)? game = null;
        var sel = Selected();
        if (sel != null && !GpuCleaner.IsProtected(sel.Name)) game = (sel.Pid, sel.Name);
        game ??= ExtremeMode.ForegroundFullscreen();
        game ??= Mon.Processes()
            .Where(p => !GpuCleaner.IsProtected(p.Name))
            .OrderByDescending(p => p.Dedicated)
            .Select(p => ((int pid, string name)?)(p.Pid, p.Name))
            .FirstOrDefault();
        if (game == null) { MessageBox.Show(this, L.T("extremeNoGame"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (MessageBox.Show(this, L.T("extremeConfirm", game.Value.name), Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        await EnterExtreme(game.Value.pid, game.Value.name);
    }

    async Task EnterExtreme(int pid, string name)
    {
        _extBtn.Enabled = false;
        var log = new List<string>();
        var st = await Task.Run(() => Extreme.Enter(pid, name, Mon, S.ExcludedProcesses, log));
        _extBtn.Enabled = true;
        AddLog(L.T("logExtremeOn", st.Closed.Count, name));
        foreach (var l in log) AddLog("  " + l);
        ApplyExtremeTexts(); Tray.ContextMenuStrip = BuildTrayMenu();
        Optimize(true);
    }

    void LeaveExtreme()
    {
        var log = Extreme.Exit(true);
        int n = log.Count(l => l.StartsWith("relaunched"));
        AddLog(L.T("logExtremeOff", n));
        foreach (var l in log.Where(l => l.Contains("failed"))) AddLog("  " + l);
        ApplyExtremeTexts(); Tray.ContextMenuStrip = BuildTrayMenu();
        if (S.ShowNotifications) Tray.ShowBalloonTip(4000, L.T("extreme"), L.T("logExtremeOff", n), ToolTipIcon.Info);
    }

    DateTime _lastFullscreenCheck = DateTime.MinValue;
    async void ExtremeTick()
    {
        if (Extreme.Active) { if (Extreme.GameExited()) LeaveExtreme(); return; }
        if (!S.ExtremeAutoFullscreen || Busy || (DateTime.Now - _lastFullscreenCheck).TotalSeconds < 5) return;
        _lastFullscreenCheck = DateTime.Now;
        var g = ExtremeMode.ForegroundFullscreen();
        if (g != null && Mon.Processes().Any(p => p.Pid == g.Value.pid && p.Dedicated > (64UL << 20))) await EnterExtreme(g.Value.pid, g.Value.name);
    }
}

public sealed partial class MainForm
{
    async void OptimizeDelayed()
    {
        if (Busy) return;
        int secs = Math.Max(3, S.OptimizeDelaySeconds);
        AddLog(L.T("delayS", secs));
        _optBtn.Enabled = false;
        await Task.Delay(secs * 1000);
        _optBtn.Enabled = true;
        Optimize(false);
    }
}
