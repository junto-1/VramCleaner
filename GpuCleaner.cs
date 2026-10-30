using System.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace AmdVramCleaner;

public sealed class CleanOptions
{
    public bool VramPressure, TrimWorkingSets, ShaderCache, StandbyList, RestartDriver;
    public HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CleanResult
{
    public GpuSample Before = new(0, 0, 0, 0), After = new(0, 0, 0, 0);
    public ulong Pressured, ShaderBytes;
    public int TrimmedProcesses;
    public List<string> Log = new();
    public long FreedBytes => (long)Before.DedicatedUsed - (long)After.DedicatedUsed;
}

public static class GpuCleaner
{
    static readonly HashSet<string> Protected = new(StringComparer.OrdinalIgnoreCase)
    { "System", "Idle", "Registry", "smss", "csrss", "wininit", "winlogon", "services", "lsass", "dwm", "Memory Compression", "fontdrvhost", "MsMpEng" };

    public static CleanResult Run(GpuMonitor monitor, CleanOptions o)
    {
        var r = new CleanResult { Before = monitor.Sample() };
        if (o.TrimWorkingSets) Try(r, "working set", () => r.TrimmedProcesses = TrimWorkingSets(monitor, o.Excluded));
        if (o.ShaderCache) Try(r, "shader cache", () => r.ShaderBytes = ClearShaderCaches());
        if (o.StandbyList) Try(r, "standby list", PurgeStandbyList);
        if (o.VramPressure && monitor.Gpu != null) Try(r, "vram pressure", () => r.Pressured = ForceEviction(monitor.Gpu, r.Log));
        if (o.RestartDriver) Try(r, "driver restart", RestartGraphicsDriver);
        Thread.Sleep(700);
        r.After = monitor.Sample();
        return r;
    }

    static void Try(CleanResult r, string step, Action a)
    {
        try { a(); r.Log.Add($"{step}: ok"); }
        catch (Exception e) { r.Log.Add($"{step}: {e.Message}"); }
    }

    // Allocates VRAM in 256 MB chunks until the adapter refuses, forcing WDDM to evict idle allocations
    // of other processes to system memory; then releases everything.
    static ulong ForceEviction(GpuInfo gpu, List<string> log)
    {
        if (gpu.DedicatedBytes == 0) { log.Add("no dedicated VRAM, skipped"); return 0; }
        using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
        if (factory.EnumAdapters1(gpu.AdapterIndex, out IDXGIAdapter1? adapter).Failure || adapter == null) return 0;
        using (adapter)
        {
            var levels = new[] { FeatureLevel.Level_11_1, FeatureLevel.Level_11_0, FeatureLevel.Level_10_0 };
            D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.None, levels, out ID3D11Device? device, out ID3D11DeviceContext? ctx).CheckError();
            using var _d = device; using var _c = ctx;
            const int chunk = 256 << 20;
            ulong cap = gpu.DedicatedBytes;
            var zero = new byte[chunk];
            var buffers = new List<ID3D11Buffer>();
            var sw = Stopwatch.StartNew();
            ulong allocated = 0;
            try
            {
                while (allocated + chunk <= cap && sw.Elapsed.TotalSeconds < 20)
                {
                    ID3D11Buffer buf;
                    try { buf = device!.CreateBuffer(new BufferDescription(chunk, BindFlags.ShaderResource, ResourceUsage.Default)); }
                    catch { break; }
                    ctx!.UpdateSubresource(zero, buf);
                    ctx.Flush();
                    buffers.Add(buf);
                    allocated += chunk;
                }
                using var q = device!.CreateQuery(new QueryDescription(QueryType.Event));
                ctx!.End(q);
                var wait = Stopwatch.StartNew();
                while (!ctx.GetData(q, out int _) && wait.ElapsedMilliseconds < 5000) Thread.Sleep(2);
            }
            finally
            {
                foreach (var b in buffers) b.Dispose();
                ctx!.ClearState();
                ctx.Flush();
            }
            log.Add($"pressured {GpuMonitor.Format(allocated)} in {sw.ElapsedMilliseconds} ms");
            return allocated;
        }
    }

    static int TrimWorkingSets(GpuMonitor monitor, HashSet<string> excluded)
    {
        int me = Environment.ProcessId, count = 0;
        foreach (var p in monitor.Processes())
        {
            if (p.Pid == me || p.Pid <= 4 || Protected.Contains(p.Name) || excluded.Contains(p.Name)) continue;
            var h = Native.OpenProcess(Native.PROCESS_QUERY_INFORMATION | Native.PROCESS_SET_QUOTA, false, p.Pid);
            if (h == IntPtr.Zero) continue;
            try { if (Native.EmptyWorkingSet(h)) count++; }
            finally { Native.CloseHandle(h); }
        }
        return count;
    }

    public static IEnumerable<string> ShaderCachePaths()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(local, "AMD", "DxCache");
        yield return Path.Combine(local, "AMD", "DxcCache");
        yield return Path.Combine(local, "AMD", "GLCache");
        yield return Path.Combine(local, "AMD", "VkCache");
        yield return Path.Combine(local, "D3DSCache");
    }

    static ulong ClearShaderCaches()
    {
        ulong freed = 0;
        foreach (var dir in ShaderCachePaths())
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                try { var len = (ulong)new FileInfo(f).Length; File.Delete(f); freed += len; } catch { }
            }
        }
        return freed;
    }

    static void PurgeStandbyList()
    {
        Native.EnablePrivilege("SeProfileSingleProcessPrivilege");
        int cmd = Native.MemoryPurgeLowPriorityStandbyList;
        Native.NtSetSystemInformation(Native.SystemMemoryListInformation, ref cmd, sizeof(int));
        cmd = Native.MemoryPurgeStandbyList;
        int status = Native.NtSetSystemInformation(Native.SystemMemoryListInformation, ref cmd, sizeof(int));
        if (status != 0) throw new InvalidOperationException($"NTSTATUS 0x{status:X8}");
    }

    static void RestartGraphicsDriver()
    {
        const byte VK_LWIN = 0x5B, VK_CONTROL = 0x11, VK_SHIFT = 0x10, VK_B = 0x42;
        const uint KEYUP = 2;
        foreach (var k in new[] { VK_LWIN, VK_CONTROL, VK_SHIFT, VK_B }) Native.keybd_event(k, 0, 0, UIntPtr.Zero);
        Thread.Sleep(80);
        foreach (var k in new[] { VK_B, VK_SHIFT, VK_CONTROL, VK_LWIN }) Native.keybd_event(k, 0, KEYUP, UIntPtr.Zero);
        Thread.Sleep(2500);
    }
}
