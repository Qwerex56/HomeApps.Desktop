namespace Jarvis.VoiceBridge.Tests;

/// <summary>A failing in-process model ends the turn with an error; the bridge never stays busy.</summary>
public sealed class LocalEngineFailureTests
{
    private static async Task SayAsync(BridgeTestBed bed)
    {
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "connected");
        bed.Wake.Say();
        bed.Mic.Push(Frames.Quiet());
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Listening, "listening");
        bed.Mic.Push(Frames.For(TimeSpan.FromSeconds(1), Frames.Speech));
        bed.Mic.Push(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet));
        await Wait.UntilAsync(() => bed.Output.Count(BridgeStates.Thinking) > 0, "thinking");
    }

    [Fact]
    public async Task A_failing_transcription_returns_the_bridge_to_idle()
    {
        await using var bed = new BridgeTestBed(linkSpeech: new ThrowingSpeechEngine(hears: false));
        await SayAsync(bed);

        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "idle after the failure");
        Assert.DoesNotContain(bed.Server.Texts, t => t.Contains("\"say\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_failing_synthesis_returns_the_bridge_to_idle_and_plays_nothing()
    {
        await using var bed = new BridgeTestBed(linkSpeech: new ThrowingSpeechEngine(hears: true));
        await SayAsync(bed);
        await Wait.UntilAsync(() => bed.Server.Texts.Any(t => t.Contains("\"say\"", StringComparison.Ordinal)), "say sent");
        var played = bed.Speaker.Played;

        await bed.Server.SendAsync("""{"type":"reply","text":"Gotowe."}""");

        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "idle after the failure");
        Assert.Equal(played, bed.Speaker.Played);
    }
}
