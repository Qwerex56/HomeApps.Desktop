namespace Jarvis.VoiceBridge;

/// <summary>One line of the action card: a tool call and its result, or a confirmation that waits (<paramref name="ConfirmBy"/>).</summary>
internal sealed record ActionView(Guid Id, string Tool, string Description, ToolCallStatus Status, DateTimeOffset? ConfirmBy);
