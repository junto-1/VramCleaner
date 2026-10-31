using System.Text.Json;

namespace AmdVramCleaner;

public sealed class Settings
{
    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AmdVramCleaner");
    static readonly string File = Path.Combine(Dir, "settings.json");

    public string Language { get; set; } = "es";
    public bool CloseToTray { get; set; } = true;
    public bool CloseAfterOptimization { get; set; }
    public bool RunOnStartup { get; set; }
    public bool RunOnLowPriority { get; set; } = true;
    public bool StartMinimized { get; set; }
    public bool ShowSharedMemory { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public bool AlwaysOnTop { get; set; }

    public bool AreaVramPressure { get; set; } = true;
    public bool AreaTrimWorkingSets { get; set; } = true;
    public bool AreaShaderCache { get; set; }
    public bool AreaStandbyList { get; set; } = true;
    public bool AreaFileCache { get; set; }
    public bool AreaRestartDriver { get; set; }
    public bool AreaRestartDwm { get; set; } = true;
    public int DwmRestartMinMb { get; set; } = 1024;

    public bool HotkeyEnabled { get; set; }
    public int HotkeyModifiers { get; set; } = 1;  // index in Modifiers list
    public string HotkeyKey { get; set; } = "G";

    public bool TrayOptimizeOnMiddleClick { get; set; }
    public bool TrayShowUsage { get; set; } = true;
    public bool TrayTransparent { get; set; }
    public string TrayBackColor { get; set; } = "#006400";
    public string TrayTextColor { get; set; } = "#FFFFFF";
    public string TrayOptimizedColor { get; set; } = "#696969";
    public int WarningLevel { get; set; } = 80;
    public string WarningColor { get; set; } = "#B8860B";
    public int DangerLevel { get; set; } = 90;
    public string DangerColor { get; set; } = "#8B0000";

    public List<string> ExcludedProcesses { get; set; } = new();

    public int AutoEveryHours { get; set; } = 1;       // 0 = disabled, 1..24
    public int AutoWhenFreeBelow { get; set; } = 20;   // 0 = disabled, percent

    public static Settings Load()
    {
        try
        {
            if (System.IO.File.Exists(File))
                return JsonSerializer.Deserialize<Settings>(System.IO.File.ReadAllText(File)) ?? new Settings();
        }
        catch { }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            System.IO.File.WriteAllText(File, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
