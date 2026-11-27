namespace HomeApps.Desktop;

/// <summary>What the tray icon shows; mapped from the bridge's state strings.</summary>
public enum TrayState
{
    Connecting,
    Idle,
    Listening,
    Thinking,
    Speaking,
    Muted,
}
