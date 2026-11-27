using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>
/// Chooses the tray icon for the bridge's state. Listening, thinking and speaking animate (frames advanced by
/// <see cref="Tick"/>); with reduced motion on, every state shows one still frame instead.
/// </summary>
public sealed class TrayIconManager(bool reducedMotion, Action<string> showIcon)
{
    private const int AnimationFrames = 4;

    private int _frame;

    public TrayState State { get; private set; } = TrayState.Connecting;

    public bool ReducedMotion { get; } = reducedMotion;

    public bool IsAnimating => !ReducedMotion && State is TrayState.Listening or TrayState.Thinking or TrayState.Speaking;

    /// <summary>The icon key now on show, e.g. "idle" or "listening-2".</summary>
    public string IconKey => IsAnimating ? $"{Name(State)}-{_frame}" : Name(State);

    public void SetState(TrayState state)
    {
        if (state == State && _frame == 0)
        {
            return;
        }

        State = state;
        _frame = 0;
        showIcon(IconKey);
    }

    /// <summary>Maps a <see cref="BridgeStates"/> string; unknown strings leave the icon as it is.</summary>
    public void SetState(string bridgeState)
    {
        if (FromBridge(bridgeState) is { } state)
        {
            SetState(state);
        }
    }

    /// <summary>Advances the animation one frame; does nothing for still states.</summary>
    public void Tick()
    {
        if (!IsAnimating)
        {
            return;
        }

        _frame = (_frame + 1) % AnimationFrames;
        showIcon(IconKey);
    }

    public static TrayState? FromBridge(string bridgeState) => bridgeState switch
    {
        BridgeStates.Connecting => TrayState.Connecting,
        BridgeStates.Idle => TrayState.Idle,
        BridgeStates.Listening => TrayState.Listening,
        BridgeStates.Thinking => TrayState.Thinking,
        BridgeStates.Speaking => TrayState.Speaking,
        BridgeStates.Muted => TrayState.Muted,
        _ => null,
    };

    private static string Name(TrayState state) => state.ToString().ToLowerInvariant();
}
