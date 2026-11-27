namespace Jarvis.VoiceBridge.Tests;

/// <summary>The stop word reacts only while JARVIS is speaking: while the server works it is ordinary (ignored) sound.</summary>
public sealed class StopWordOutsideSpeechTests
{
    [Fact]
    public async Task A_stop_said_while_the_server_works_is_not_transcribed_nor_a_cancel()
    {
        await using var bed = new BridgeTestBed("stop");
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();

        bed.Mic.Push(Frames.For(TimeSpan.FromMilliseconds(400), Frames.Speech));
        bed.Mic.Push(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet));
        await Wait.SettleAsync();

        Assert.Equal(0, bed.Speech.Transcribed);
        Assert.Equal(BridgeStates.Thinking, bed.Output.Last);
        Assert.DoesNotContain(bed.Server.Texts, t => t.Contains("\"cancel\"", StringComparison.Ordinal));
    }
}
