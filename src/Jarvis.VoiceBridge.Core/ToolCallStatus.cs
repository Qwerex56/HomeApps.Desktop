namespace Jarvis.VoiceBridge;

/// <summary>What became of a tool call JARVIS made (the server's <c>ToolCallStatus</c>).</summary>
internal enum ToolCallStatus
{
    Done = 0,
    Failed = 1,
    NeedsConfirmation = 2,
    Denied = 3,
    Expired = 4,
}
