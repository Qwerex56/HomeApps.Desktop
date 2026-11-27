namespace Jarvis.VoiceBridge.Tests;

/// <summary>Records what would be played; with <see cref="Block"/> a sound lasts until it is cancelled.</summary>
internal sealed class FakeSpeaker : ISpeaker
{
    private int _played;
    private int _cancelled;

    public volatile bool Block;

    /// <summary>How long a blocking sound lasts when it is not cancelled (infinite by default).</summary>
    public TimeSpan Hold { get; set; } = Timeout.InfiniteTimeSpan;

    public int Played => _played;

    public int Cancelled => _cancelled;

    public async Task PlayAsync(byte[] wav, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _played);
        if (Block)
        {
            try
            {
                await Task.Delay(Hold, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref _cancelled);
                throw;
            }
        }
    }
}
