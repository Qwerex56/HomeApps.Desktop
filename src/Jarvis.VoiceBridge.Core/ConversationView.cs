namespace Jarvis.VoiceBridge;

internal sealed record ConversationView(Guid Id, string Title, DateTimeOffset LastActivityAt)
{
    public override string ToString() => Title;
}
