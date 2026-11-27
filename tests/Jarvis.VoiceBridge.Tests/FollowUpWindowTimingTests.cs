namespace Jarvis.VoiceBridge.Tests;

/// <summary>D11 timing with frames as the clock: the 8 s window is measured from the end of the answer, not from the wake word.</summary>
public sealed class FollowUpWindowTimingTests
{
    [Fact]
    public void Nothing_said_closes_the_window_at_exactly_eight_seconds()
    {
        var speech = new EndOfSpeech(FollowUpWindow.Length);
        var states = Frames.For(TimeSpan.FromSeconds(9), Frames.Quiet).Select(speech.Push).ToList();

        Assert.Equal(SpeechState.Going, states[98]);
        Assert.Equal(SpeechState.NothingSaid, states[99]);
    }

    [Fact]
    public void Speech_at_7_9_seconds_keeps_the_window_open_and_ends_after_its_own_silence()
    {
        var speech = new EndOfSpeech(FollowUpWindow.Length);
        var states = Frames.For(TimeSpan.FromSeconds(7.9), Frames.Quiet)
            .Concat(Frames.For(TimeSpan.FromMilliseconds(400), Frames.Speech))
            .Concat(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet))
            .Select(speech.Push)
            .ToList();

        Assert.DoesNotContain(SpeechState.NothingSaid, states);
        Assert.Contains(SpeechState.Ended, states);
    }
}
