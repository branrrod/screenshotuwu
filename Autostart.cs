using Microsoft.Win32;

namespace Screenshotuwu;

/// <summary>Per-user "Start with Windows" via HKCU\...\Run. The user's choice lives in HKCU\Software\Screenshotuwu.</summary>
internal static class Autostart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string SettingsKeyPath = @"Software\Screenshotuwu";
    private const string ValueName = "Screenshotuwu";
    private const string EnabledFlag = "StartWithWindows";

    /// <summary>Quoted path of the running exe, as written to the Run value.</summary>
    public static string Command => $"\"{Environment.ProcessPath}\"";

    /// <summary>The persisted choice; null if the user has never run the app.</summary>
    public static bool? StoredChoice
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath);
            return key?.GetValue(EnabledFlag) is int v ? v != 0 : null;
        }
    }

    /// <summary>Called once at launch: on by default on first run, otherwise honour the stored choice and refresh a moved exe path.</summary>
    public static bool Sync()
    {
        bool enabled = StoredChoice ?? true;
        SetEnabled(enabled);
        return enabled;
    }

    public static void SetEnabled(bool enabled)
    {
        using (RegistryKey settings = Registry.CurrentUser.CreateSubKey(SettingsKeyPath))
            settings.SetValue(EnabledFlag, enabled ? 1 : 0, RegistryValueKind.DWord);

        using RegistryKey run = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            if (Environment.ProcessPath is not null && run.GetValue(ValueName) as string != Command)
                run.SetValue(ValueName, Command, RegistryValueKind.String);
        }
        else
        {
            run.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
