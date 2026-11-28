using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>Requirements 2.3 and 2.4: every action that waits for a confirmation is shown once with Tak / Nie; the answer goes to the server with the person's token. Never decided by voice (D8).</summary>
internal sealed class ConfirmationNotifier(JarvisApi api, INotifier notifier) : IDisposable
{
    private readonly HashSet<Guid> _shown = [];
    private readonly SemaphoreSlim _gate = new(1, 1);

    public void Dispose() => _gate.Dispose();

    /// <summary>Reads what waits and asks about each action not shown yet; returns how many were decided.</summary>
    public async Task<int> CheckAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
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
        finally
        {
            _gate.Release();
        }
    }
}
