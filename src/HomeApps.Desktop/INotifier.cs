using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>Shows a system notification with Tak / Nie for a waiting action and returns the person's choice (null: dismissed).</summary>
internal interface INotifier
{
    Task<bool?> AskAsync(ActionView action, CancellationToken cancellationToken);
}
