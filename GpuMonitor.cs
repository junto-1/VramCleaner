using System.Diagnostics;
using System.Text.RegularExpressions;
using Vortice.DXGI;

namespace AmdVramCleaner;

public sealed record GpuInfo(string Name, uint VendorId, ulong DedicatedBytes, ulong SharedBytes, string Luid, uint AdapterIndex)
{
    public bool IsAmd => VendorId == 0x1002;
}

public sealed record GpuSample(ulong DedicatedUsed, ulong DedicatedTotal, ulong SharedUsed, ulong SharedTotal)
{
    public double DedicatedPercent => DedicatedTotal == 0 ? 0 : 100.0 * DedicatedUsed / DedicatedTotal;
    public double SharedPercent => SharedTotal == 0 ? 0 : 100.0 * SharedUsed / SharedTotal;
}

public sealed record GpuProcessUsage(int Pid, string Name, ulong Dedicated, ulong Shared);

public sealed class GpuMonitor
{
    static readonly Regex PidRx = new(@"^pid_(\d+)_luid_(0x[0-9A-Fa-f]+_0x[0-9A-Fa-f]+)", RegexOptions.Compiled);
    readonly PerformanceCounterCategory? _adapterCat, _processCat;

    public List<GpuInfo> Gpus { get; } = new();
    public GpuInfo? Gpu { get; }
    public string DriverVersion { get; } = "";

    public GpuMonitor()
    {
        try
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1? adapter).Success; i++)
            {
                using (adapter)
                {
                    var d = adapter!.Description1;
                    if ((d.Flags & AdapterFlags.Software) != 0) continue;
                    long lv = (long)d.Luid; string luid = $"0x{(uint)(lv >> 32):X8}_0x{(uint)lv:X8}";
                    Gpus.Add(new GpuInfo(d.Description, d.VendorId, (ulong)d.DedicatedVideoMemory, (ulong)d.SharedSystemMemory, luid, i));
                }
            }
        }
        catch { }
        Gpu = Gpus.FirstOrDefault(g => g.IsAmd) ?? Gpus.OrderByDescending(g => g.DedicatedBytes).FirstOrDefault();
        try { _adapterCat = new PerformanceCounterCategory("GPU Adapter Memory"); } catch { }
        try { _processCat = new PerformanceCounterCategory("GPU Process Memory"); } catch { }
        DriverVersion = ReadDriverVersion();
    }

    string ReadDriverVersion()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            if (key == null) return "";
            foreach (var sub in key.GetSubKeyNames())
            {
                using var k = key.OpenSubKey(sub);
                var desc = k?.GetValue("DriverDesc") as string;
                if (desc != null && Gpu != null && desc.Equals(Gpu.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var radeon = k!.GetValue("RadeonSoftwareVersion") as string;
                    var ver = k.GetValue("DriverVersion") as string ?? "";
                    return radeon != null ? $"{radeon} ({ver})" : ver;
                }
            }
        }
        catch { }
        return "";
    }

    public GpuSample Sample()
    {
        ulong ded = 0, shared = 0;
        if (_adapterCat != null)
        {
            try
            {
                var data = _adapterCat.ReadCategory();
                ded = Sum(data, "Dedicated Usage");
                shared = Sum(data, "Shared Usage");
            }
            catch { }
        }
        ulong total = Gpu?.DedicatedBytes ?? 0;
        ulong sharedTotal = Gpu?.SharedBytes ?? 0;
        if (total > 0) ded = Math.Min(ded, total);
        if (sharedTotal > 0) shared = Math.Min(shared, sharedTotal);
        return new GpuSample(ded, total, shared, sharedTotal);
    }

    ulong Sum(InstanceDataCollectionCollection data, string counter)
    {
        if (!data.Contains(counter)) return 0;
        ulong matched = 0, all = 0;
        bool any = false;
        foreach (InstanceData d in data[counter].Values)
        {
            ulong v = (ulong)Math.Max(0, d.RawValue);
            all += v;
            if (Gpu != null && d.InstanceName.Contains(Gpu.Luid, StringComparison.OrdinalIgnoreCase)) { matched += v; any = true; }
        }
        return any ? matched : all;
    }

    public List<GpuProcessUsage> Processes()
    {
        var acc = new Dictionary<int, ulong[]>();
        if (_processCat == null) return new();
        try
        {
            var data = _processCat.ReadCategory();
            Accumulate(data, "Dedicated Usage", acc, 0);
            Accumulate(data, "Shared Usage", acc, 1);
        }
        catch { }
        var result = new List<GpuProcessUsage>();
        foreach (var (pid, v) in acc)
        {
            if (v[0] == 0 && v[1] == 0) continue;
            string name;
            try { name = Process.GetProcessById(pid).ProcessName; } catch { continue; }
            result.Add(new GpuProcessUsage(pid, name, v[0], v[1]));
        }
        return result.OrderByDescending(p => p.Dedicated).ToList();
    }

    void Accumulate(InstanceDataCollectionCollection data, string counter, Dictionary<int, ulong[]> acc, int idx)
    {
        if (!data.Contains(counter)) return;
        foreach (InstanceData d in data[counter].Values)
        {
            var m = PidRx.Match(d.InstanceName);
            if (!m.Success) continue;
            if (Gpu != null && !m.Groups[2].Value.Equals(Gpu.Luid, StringComparison.OrdinalIgnoreCase)) continue;
            int pid = int.Parse(m.Groups[1].Value);
            if (!acc.TryGetValue(pid, out var arr)) acc[pid] = arr = new ulong[2];
            arr[idx] += (ulong)Math.Max(0, d.RawValue);
        }
    }

    public static string Format(ulong bytes)
    {
        if (bytes >= 1UL << 30) return $"{bytes / (double)(1UL << 30):0.#} GB";
        if (bytes >= 1UL << 20) return $"{bytes / (double)(1UL << 20):0} MB";
        return $"{bytes / 1024.0:0} KB";
    }
}
