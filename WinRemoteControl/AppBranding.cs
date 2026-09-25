namespace WinRemoteControl;

public static class AppBranding
{
#if DEBUG
    public const string DisplayName = "WinRemoteControl DEBUG";
#else
    public const string DisplayName = "WinRemoteControl";
#endif
    // Configuration and the single-instance identity are deliberately independent of build branding.
    public const string InstanceMutexName = @"Local\WinRemoteControl.SharedConfiguration";

    public static Icon CreateIcon()
    {
        using var stream = typeof(AppBranding).Assembly.GetManifestResourceStream("WinRemoteControl.AppIcon.ico")
            ?? throw new InvalidOperationException("Application icon is missing.");
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }
}
