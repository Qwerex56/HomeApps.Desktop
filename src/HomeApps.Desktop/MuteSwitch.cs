namespace HomeApps.Desktop;

/// <summary>The microphone mute toggled by the global hotkey and the tray menu; the tray shows it as <see cref="TrayState.Muted"/>.</summary>
public sealed class MuteSwitch(Action<bool> changed)
{
    public bool IsMuted { get; private set; }

    public void Toggle()
    {
        IsMuted = !IsMuted;
        changed(IsMuted);
    }
}
