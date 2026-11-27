using SharpHook;
using SharpHook.Data;

namespace HomeApps.Desktop;

/// <summary>The system-wide mute hotkey (Ctrl+Alt+M), caught by SharpHook even when no window has focus.</summary>
public sealed class GlobalHotkey : IDisposable
{
    private readonly TaskPoolGlobalHook _hook = new();

    public GlobalHotkey(Action pressed)
    {
        _hook.KeyPressed += (_, e) =>
        {
            var mask = e.RawEvent.Mask;
            if (e.Data.KeyCode == KeyCode.VcM && mask.HasFlag(EventMask.Ctrl) && mask.HasFlag(EventMask.Alt))
            {
                pressed();
            }
        };
    }

    public void Start() => _ = _hook.RunAsync();

    public void Dispose() => _hook.Dispose();
}
