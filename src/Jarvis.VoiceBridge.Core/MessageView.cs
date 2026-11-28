namespace Jarvis.VoiceBridge;

/// <param name="Channel"><c>app</c>, <c>voice</c> or <c>desktop</c>.</param>
internal sealed record MessageView(Guid Id, MessageRole Role, string Text, DateTimeOffset SentAt, IReadOnlyList<ActionView> Actions, string Channel = "app");
