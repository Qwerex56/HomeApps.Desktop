using SharpHook;
using SharpHook.Data;

namespace HomeApps.Desktop;

/// <summary>The system-wide mute hotkey (Ctrl+Alt+M), caught by SharpHook even when no window has focus.</summary>
public sealed class GlobalHotkey : IDisposable
{
    private readonly TaskPoolGlobalHook _hook = new();
    private int _disposed;

    public GlobalHotkey(Action pressed)
    {
        _hook.KeyPressed += (_, e) =>
        {
            if (IsMuteShortcut(e.Data.KeyCode, e.RawEvent.Mask))
            {
                pressed();
            }
        };
    }

    /// <summary>Ctrl+Alt+M, with either the left or the right Ctrl and Alt (the masks are composites of both sides).</summary>
    public static bool IsMuteShortcut(KeyCode key, EventMask mask) =>
        key == KeyCode.VcM && (mask & EventMask.Ctrl) != 0 && (mask & EventMask.Alt) != 0;

    public void Start() =>
        _ = _hook.RunAsync().ContinueWith(
            task => Console.Error.WriteLine($"Skrót wyciszania nie działa: {task.Exception?.GetBaseException().Message}"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _hook.Dispose();
        }
    }
}
