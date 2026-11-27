namespace Jarvis.VoiceBridge.Tests;

/// <summary>D11: the 8 s window opens after EVERY finished answer, without the wake word.</summary>
public sealed class FollowUpWindowTests
{
    [Fact]
    public void The_window_is_eight_seconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(8), FollowUpWindow.Length);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void It_opens_unless_cancelled_or_muted(bool cancelled, bool muted, bool opens)
    {
        Assert.Equal(opens, FollowUpWindow.Opens(cancelled, muted));
    }

    [Fact]
    public void Eight_quiet_seconds_close_the_window_but_less_do_not()
    {
        var speech = new EndOfSpeech(FollowUpWindow.Length);
        var states = Frames.For(TimeSpan.FromSeconds(8.2), Frames.Quiet).Select(speech.Push).ToList();

        Assert.Equal(SpeechState.Going, states[(int)(7_800 / 80)]);
        Assert.Contains(SpeechState.NothingSaid, states);
    }

    [Fact]
    public async Task After_a_plain_answer_the_bridge_listens_again_without_the_wake_word_and_then_goes_idle()
    {
        await using var bed = new BridgeTestBed();
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();

        await bed.Server.SendAsync("""{"type":"reply","text":"Gotowe.","awaiting":false}""");
        await bed.Server.SendAsync("""{"type":"done"}""");
        await Wait.UntilAsync(() => bed.Output.Count(BridgeStates.Listening) == 2, "the follow-up window opens");

        bed.Mic.Push(Frames.For(TimeSpan.FromSeconds(8.2), Frames.Quiet));
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "the window closes after 8 s of silence");
        Assert.Contains("""{"type":"cancel"}""", bed.Server.Texts);
    }

    [Fact]
    public async Task What_is_said_in_the_window_is_sent_without_the_wake_word()
    {
        await using var bed = new BridgeTestBed();
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();
        await bed.Server.SendAsync("""{"type":"done"}""");
        await Wait.UntilAsync(() => bed.Output.Count(BridgeStates.Listening) == 2, "the follow-up window opens");

        var before = bed.Server.Binary;
        bed.Mic.Push(Frames.For(TimeSpan.FromSeconds(1), Frames.Speech));
        await Wait.UntilAsync(() => bed.Server.Binary > before, "follow-up audio reaches the server");
    }

    [Fact]
    public async Task The_window_opens_only_after_the_sentence_that_precedes_done_has_been_played()
    {
        await using var bed = new BridgeTestBed();
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();
        bed.Speaker.Block = true;
        bed.Speaker.Hold = TimeSpan.FromMilliseconds(1200);

        await bed.Server.SendAsync("""{"type":"audio","format":"wav"}""");
        await bed.Server.SendBinaryAsync(FakeSpeechEngine.Silence());
        await bed.Server.SendAsync("""{"type":"done"}""");
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Speaking, "speaking");
        await Task.Delay(500);

        Assert.Equal(1, bed.Output.Count(BridgeStates.Listening));
        await Wait.UntilAsync(() => bed.Output.Count(BridgeStates.Listening) == 2, "the window opens after the playback");
    }
}
