namespace Jarvis.VoiceBridge.Tests;

public sealed class StopWordRulesTests
{
    [Theory]
    [InlineData("stop", true)]
    [InlineData("Jarvis, stop!", true)]
    [InlineData("cisza proszę teraz", true)]
    [InlineData("nie mów stop do mnie teraz", false)]
    [InlineData("on powiedział że cisza jest ważna", false)]
    public void Only_short_utterances_with_a_stop_word_count(string text, bool stop)
    {
        Assert.Equal(stop, StopWords.IsStop(text));
    }

    [Fact]
    public void A_single_loud_frame_is_not_sent_to_the_model()
    {
        var engine = new FakeSpeechEngine("stop");
        var listener = new StopWordListener(engine.TranscribeAsync, () => TimeSpan.FromSeconds(1));
        listener.Begin(FakeSpeechEngine.Silence());

        var heard = Frames.For(TimeSpan.FromMilliseconds(80), Frames.Speech)
            .Concat(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet))
            .Select(listener.Push)
            .Any(pcm => pcm is not null);

        Assert.False(heard);
    }

    [Fact]
    public void Two_speech_frames_are_enough()
    {
        var listener = new StopWordListener(new FakeSpeechEngine("stop").TranscribeAsync, () => TimeSpan.FromSeconds(1));
        listener.Begin(FakeSpeechEngine.Silence());

        var heard = Frames.For(TimeSpan.FromMilliseconds(160), Frames.Speech)
            .Concat(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet))
            .Select(listener.Push)
            .Any(pcm => pcm is not null);

        Assert.True(heard);
    }

    [Fact]
    public void The_player_is_chosen_per_system()
    {
        Assert.Equal("afplay", SystemSpeaker.Command("a.wav", windows: false, mac: true).FileName);
        Assert.Equal("aplay", SystemSpeaker.Command("a.wav", windows: false, mac: false).FileName);
        var windows = SystemSpeaker.Command("it's.wav", windows: true, mac: false);
        Assert.Equal("powershell", windows.FileName);
        Assert.Contains("PlaySync", windows.ArgumentList[^1], StringComparison.Ordinal);
        Assert.Contains("it''s.wav", windows.ArgumentList[^1], StringComparison.Ordinal);
    }
}
