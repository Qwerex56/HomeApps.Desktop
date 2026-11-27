namespace Jarvis.VoiceBridge;

/// <summary>An action JARVIS took or wants confirmed in his reply, e.g. ("pending", "Dodaj mleko do listy").</summary>
internal sealed record ReplyAction(string Status, string Description);
