using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>Requirements 2.3 and 2.4: every action that waits for a confirmation is shown once with Tak / Nie; the answer goes to the server with the person's token. Never decided by voice (D8).</summary>
internal sealed class ConfirmationNotifier(JarvisApi api, INotifier notifier)
{
    private readonly HashSet<Guid> _shown = [];

    /// <summary>Reads what waits and asks about each action not shown yet; returns how many were decided.</summary>
    public async Task<int> CheckAsync(CancellationToken cancellationToken)
    {
        var decided = 0;
        foreach (var action in (await api.PendingAsync(cancellationToken)).Where(a => a.Status == ToolCallStatus.NeedsConfirmation))
        {
            if (!_shown.Add(action.Id))
            {
                continue;
            }

            if (await notifier.AskAsync(action, cancellationToken) is { } approve)
            {
                await api.DecideAsync(action.Id, approve, cancellationToken);
                decided++;
            }
        }

        return decided;
    }
}
