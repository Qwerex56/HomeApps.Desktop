namespace Jarvis.VoiceBridge.Tests;

/// <summary>Q2(A): a lease_conflict that arrives while this computer is already speaking must still silence it at once.</summary>
public sealed class LeaseConflictDuringSpeechTests
{
    [Fact]
    public async Task A_lease_conflict_during_playback_stops_the_sound_and_goes_idle_at_once()
    {
        await using var bed = new BridgeTestBed();
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();
        bed.Speaker.Block = true;
        bed.Speaker.Hold = TimeSpan.FromSeconds(3);

        await bed.Server.SendAsync("""{"type":"audio","format":"wav"}""");
        await bed.Server.SendBinaryAsync(FakeSpeechEngine.Silence());
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Speaking, "speaking");

        var conflict = System.Diagnostics.Stopwatch.StartNew();
        await bed.Server.SendAsync("""{"type":"lease_conflict","code":"turn_in_progress"}""");
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "idle after lease_conflict during playback");

        // The sentence lasts 3 s; a conflict must not wait for it to finish.
        Assert.True(conflict.Elapsed < TimeSpan.FromSeconds(1), $"lease_conflict handled after {conflict.Elapsed.TotalSeconds:0.0} s");
        Assert.Equal(1, bed.Speaker.Cancelled);
    }
}
