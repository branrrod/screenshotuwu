namespace Screenshotuwu;

static class Program
{
    [STAThread]
    static void Main()
    {
        // One tray icon per user session: autostart plus a manual launch must not stack.
        using var mutex = new Mutex(initiallyOwned: true, @"Local\Screenshotuwu.SingleInstance", out bool createdNew);
        if (!createdNew)
            return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
