using System.Diagnostics;
using System.Text.Json;

namespace AmdVramCleaner;

public sealed class ExtremeMode
{
    static readonly HashSet<string> Keep = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "AmdVramCleaner", "RadeonSoftware", "AMDRSServ", "AMDRSSrcExt", "amdow", "atieclxx", "atiesrxx", "AMDLinkUpdate",
        "steam", "steamwebhelper", "steamservice", "EpicGamesLauncher", "EpicWebHelper", "Battle.net", "Agent", "GalaxyClient", "GalaxyClientService",
        "EADesktop", "EABackgroundService", "RiotClientServices", "RiotClientUx", "UbisoftConnect", "upc", "UplayWebCore", "PlayniteUI", "Playnite",
        "EasyAntiCheat", "EasyAntiCheat_EOS", "BEService", "vgtray", "vgc", "FACEIT",
        "GameBar", "GameBarFTServer", "GamingServices", "GamingServicesNet",
    };
    public static readonly string StateFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmdVramCleaner", "extreme-session.json");

    public sealed record ClosedApp(string Name, string Path, string[] Args);
    public sealed class State { public int GamePid; public string GameName = ""; public List<ClosedApp> Closed = new(); public DateTime Started = DateTime.Now; }

    public State? Current { get; private set; }
    public bool Active => Current != null;

    public static State? LoadPending()
    {
        try { return File.Exists(StateFile) ? JsonSerializer.Deserialize<State>(File.ReadAllText(StateFile)) : null; } catch { return null; }
    }

    public static (int pid, string name)? ForegroundFullscreen()
    {
        var h = Native.GetForegroundWindow();
        if (h == IntPtr.Zero || !Native.GetWindowRect(h, out var r)) return null;
        var b = Screen.FromHandle(h).Bounds;
        if (r.Right - r.Left < b.Width || r.Bottom - r.Top < b.Height) return null;
        Native.GetWindowThreadProcessId(h, out uint pid);
        if (pid == 0) return null;
        try { using var p = Process.GetProcessById((int)pid); return GpuCleaner.IsProtected(p.ProcessName) || Keep.Contains(p.ProcessName) ? null : ((int)pid, p.ProcessName); } catch { return null; }
    }

    public State Enter(int gamePid, string gameName, GpuMonitor mon, IEnumerable<string> excluded, List<string> log)
    {
        var st = new State { GamePid = gamePid, GameName = gameName };
        string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        int me = Environment.ProcessId, session = Process.GetCurrentProcess().SessionId;
        var gpuPids = mon.Processes().Select(p => p.Pid).ToHashSet();
        var ex = new HashSet<string>(excluded, StringComparer.OrdinalIgnoreCase);
        var victims = new List<Process>();
        var skip = new Dictionary<string, int>(); void Skip(string why) => skip[why] = skip.TryGetValue(why, out var c) ? c + 1 : 1;
        int scanned = 0;
        foreach (var p in Process.GetProcesses())
        {
            scanned++;
            try
            {
                if (p.Id == me || p.Id == gamePid || p.Id <= 4 || p.SessionId != session) { Skip("system/other-session"); continue; }
                if (GpuCleaner.IsProtected(p.ProcessName) || Keep.Contains(p.ProcessName) || ex.Contains(p.ProcessName)) { Skip("protected"); continue; }
                bool hasWindow = false; try { hasWindow = p.MainWindowHandle != IntPtr.Zero; } catch { }
                if (!hasWindow && !gpuPids.Contains(p.Id)) { Skip("no-window/no-vram"); continue; }
                string path = ""; try { path = p.MainModule?.FileName ?? ""; } catch { Skip("access-denied"); continue; }
                if (path.Length == 0 || path.StartsWith(windir, StringComparison.OrdinalIgnoreCase)) { Skip("system-path"); continue; }
                try { if (Process.GetProcessById(gamePid).MainModule?.FileName is string gp && Path.GetDirectoryName(gp) is string gd && path.StartsWith(gd, StringComparison.OrdinalIgnoreCase)) continue; } catch { }
                if (!st.Closed.Any(c => c.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) st.Closed.Add(new ClosedApp(p.ProcessName, path, p.StartInfo.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
                victims.Add(p);
            }
            catch { }
        }
        foreach (var p in victims) { try { p.CloseMainWindow(); } catch { } }
        log.Add($"extreme scan: {scanned} procesos, {victims.Count} a cerrar (" +
                string.Join(", ", skip.Select(kv => $"{kv.Key}={kv.Value}")) + ")");
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < 5000 && victims.Any(p => { try { return !p.HasExited; } catch { return false; } })) Thread.Sleep(200);
        foreach (var p in victims) { try { if (!p.HasExited) p.Kill(true); log.Add($"closed {p.ProcessName}"); } catch { } p.Dispose(); }
        Current = st; Save();
        return st;
    }

    public bool GameExited()
    {
        if (Current == null) return false;
        try { using var p = Process.GetProcessById(Current.GamePid); return p.HasExited; } catch { return true; }
    }

    public List<string> Exit(bool relaunch)
    {
        var st = Current; Current = null;
        try { File.Delete(StateFile); } catch { }
        var log = new List<string>();
        if (st == null || !relaunch) return log;
        foreach (var a in st.Closed)
        {
            try
            {
                if (Process.GetProcessesByName(a.Name).Length > 0) continue;
                Process.Start(new ProcessStartInfo(a.Path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(a.Path) ?? "", Arguments = string.Join(" ", a.Args) });
                log.Add($"relaunched {a.Name}");
            }
            catch (Exception e) { log.Add($"relaunch failed {a.Name}: {e.Message}"); }
        }
        return log;
    }

    public void Restore(State st) { Current = st; }

    void Save()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(StateFile)!); File.WriteAllText(StateFile, JsonSerializer.Serialize(Current)); } catch { }
    }
}
