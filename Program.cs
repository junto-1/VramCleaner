namespace AmdVramCleaner;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length > 0) return Cli.Run(args);
        using var mutex = new Mutex(true, "AmdVramCleaner_SingleInstance", out bool created);
        if (!created) return 0;
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}

// Command line mode (inspired by GpuClean):
//   AmdVramCleaner --status | --clear [--dry-run] [--exclude a,b] [--no-pressure] [--shaders] [--driver] [--dwm|--no-dwm] | --kill <pid|name> [--force] | --help
static class Cli
{
    public static int Run(string[] args)
    {
        Native.AttachConsole(-1);
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch { }
        var sw = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(sw);
        Console.WriteLine();
        var set = new HashSet<string>(args.Where(a => a.StartsWith("--")), StringComparer.OrdinalIgnoreCase);
        string? Arg(string name) { int i = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase)); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
        var s = Settings.Load();
        L.Lang = Arg("--lang") ?? s.Language;
        var mon = new GpuMonitor();
        int code = 0;
        try
        {
            if (set.Contains("--help") || set.Contains("-h"))
                Console.WriteLine("AmdVramCleaner --status | --clear [--dry-run] [--exclude a,b] [--no-pressure] [--shaders] [--driver] [--dwm|--no-dwm] | --kill <pid|name> [--force]");
            else if (set.Contains("--status")) Status(mon);
            else if (set.Contains("--kill")) code = Kill(mon, Arg("--kill"), set.Contains("--force"));
            else if (set.Contains("--clear"))
            {
                var o = new CleanOptions
                {
                    VramPressure = s.AreaVramPressure && !set.Contains("--no-pressure"), TrimWorkingSets = true,
                    ShaderCache = s.AreaShaderCache || set.Contains("--shaders"), StandbyList = s.AreaStandbyList, FileCache = s.AreaFileCache,
                    RestartDriver = set.Contains("--driver"),
                    RestartDwm = (s.AreaRestartDwm || set.Contains("--dwm")) && !set.Contains("--no-dwm") && (set.Contains("--dwm") || ExtremeMode.ForegroundFullscreen() == null), DwmMinBytes = set.Contains("--dwm") ? 0 : (ulong)s.DwmRestartMinMb << 20,
                    Excluded = new HashSet<string>(s.ExcludedProcesses.Concat((Arg("--exclude") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)), StringComparer.OrdinalIgnoreCase),
                };
                if (set.Contains("--dry-run")) { Console.WriteLine(L.T("dryRunTitle")); foreach (var l in GpuCleaner.Plan(mon, o)) Console.WriteLine(l); }
                else
                {
                    var r = GpuCleaner.Run(mon, o);
                    Console.WriteLine(L.T("done", GpuMonitor.Format(r.Before.DedicatedUsed), GpuMonitor.Format(r.After.DedicatedUsed), GpuMonitor.Format((ulong)Math.Max(0, r.FreedBytes)), r.TrimmedProcesses, GpuMonitor.Format(r.ShaderBytes)));
                    foreach (var l in r.Log) Console.WriteLine("  " + l);
                }
            }
            else { Console.WriteLine("Unknown arguments. Use --help"); code = 2; }
        }
        catch (Exception e) { Console.WriteLine("Error: " + e.Message); code = 1; }
        Native.FreeConsole();
        return code;
    }

    static void Status(GpuMonitor mon)
    {
        var g = mon.Gpu; var smp = mon.Sample();
        Console.WriteLine(g == null ? L.T("noGpu") : L.T("gpu", g.Name, mon.DriverVersion == "" ? "?" : mon.DriverVersion));
        Console.WriteLine($"VRAM {GpuMonitor.Format(smp.DedicatedUsed)} / {GpuMonitor.Format(smp.DedicatedTotal)} ({smp.DedicatedPercent:0}%)   Shared {GpuMonitor.Format(smp.SharedUsed)} / {GpuMonitor.Format(smp.SharedTotal)}");
        Console.WriteLine($"{"PID",7}  {"VRAM",10}  {"Shared",10}  Process");
        foreach (var p in mon.Processes()) Console.WriteLine($"{p.Pid,7}  {GpuMonitor.Format(p.Dedicated),10}  {GpuMonitor.Format(p.Shared),10}  {p.Name}");
    }

    static int Kill(GpuMonitor mon, string? target, bool force)
    {
        if (target == null) { Console.WriteLine("--kill <pid|name>"); return 2; }
        var victims = mon.Processes().Where(p => p.Pid.ToString() == target || p.Name.Equals(target, StringComparison.OrdinalIgnoreCase) || p.Name.Equals(Path.GetFileNameWithoutExtension(target), StringComparison.OrdinalIgnoreCase)).ToList();
        if (victims.Count == 0) { Console.WriteLine("No GPU process matches: " + target); return 1; }
        int fails = 0;
        foreach (var v in victims)
        {
            if (GpuCleaner.IsProtected(v.Name)) { Console.WriteLine($"skip protected {v.Name}"); continue; }
            bool ok = GpuCleaner.KillProcess(v.Pid, force);
            Console.WriteLine(ok ? L.T("logKilled", v.Name, v.Pid) : L.T("killFail", v.Name) + (force ? "" : "  (try --force)"));
            if (!ok) fails++;
        }
        return fails == 0 ? 0 : 1;
    }
}
