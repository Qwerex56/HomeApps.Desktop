namespace Jarvis.VoiceBridge;

/// <summary>The states the bridge reports (<see cref="IBridgeOutput.State"/>), as the hologram shows them.</summary>
internal static class BridgeStates
{
    public const string Connecting = "connecting";
    public const string Idle = "idle";
    public const string Listening = "listening";
    public const string Thinking = "thinking";
    public const string Speaking = "speaking";
    public const string Muted = "muted";
}
