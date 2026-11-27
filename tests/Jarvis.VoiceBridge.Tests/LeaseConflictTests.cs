namespace Jarvis.VoiceBridge.Tests;

/// <summary>Q2(A): {"type":"lease_conflict","code":"turn_in_progress"} from <c>/jarvis/voice</c>: silent and idle at once.</summary>
public sealed class LeaseConflictTests
{
    private const string Conflict = """{"type":"lease_conflict","code":"turn_in_progress"}""";

    [Fact]
    public async Task While_recording_the_bridge_goes_idle_stops_sending_and_plays_nothing()
    {
        await using var bed = new BridgeTestBed();
        await bed.SpeakToJarvisAsync();
        var played = bed.Speaker.Played;

        await bed.Server.SendAsync(Conflict);
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "idle after lease_conflict");
        await Wait.SettleAsync();
        var sent = bed.Server.Binary;
        bed.Mic.Push(Frames.For(TimeSpan.FromSeconds(1), Frames.Speech));
        await Wait.SettleAsync();

        Assert.Equal(sent, bed.Server.Binary);
        Assert.Equal(played, bed.Speaker.Played);
        Assert.DoesNotContain(bed.Server.Texts, t => t.Contains("\"stop\"", StringComparison.Ordinal) || t.Contains("\"cancel\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task While_the_server_works_a_later_audio_is_not_played()
    {
        await using var bed = new BridgeTestBed();
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();
        await Wait.SettleAsync();
        var played = bed.Speaker.Played;

        await bed.Server.SendAsync(Conflict);
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "idle after lease_conflict");
        await bed.Server.SendAsync("""{"type":"audio","format":"wav"}""");
        await bed.Server.SendBinaryAsync(FakeSpeechEngine.Silence());
        await Wait.SettleAsync();

        Assert.Equal(played, bed.Speaker.Played);
        Assert.Equal(BridgeStates.Idle, bed.Output.Last);
        Assert.DoesNotContain(BridgeStates.Speaking, bed.Output.States);
    }
}
