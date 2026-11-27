namespace Jarvis.VoiceBridge;

/// <summary>
/// D11 (requirement 2.1): after EVERY finished answer the microphone stays open for <see cref="Length"/> without the
/// wake word, not only when JARVIS waits for an action's answer.
/// </summary>
internal static class FollowUpWindow
{
    public static readonly TimeSpan Length = TimeSpan.FromSeconds(8);

    /// <summary>The window opens unless the turn was cancelled or the microphone is muted.</summary>
    public static bool Opens(bool cancelled, bool muted) => !cancelled && !muted;
}
