namespace AmdVramCleaner;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "AmdVramCleaner_SingleInstance", out bool created);
        if (!created) return;
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
