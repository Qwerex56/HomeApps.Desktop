namespace Jarvis.VoiceBridge.Tests;

/// <summary>Stop words during TTS, through the echo gate; no real model or microphone.</summary>
public sealed class StopWordInterruptionTests
{
    [Theory]
    [InlineData("Stop!", true)]
    [InlineData("cisza", true)]
    [InlineData("Cisza, proszę", true)]
    [InlineData("tak", false)]
    [InlineData("nie", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Only_stop_and_cisza_are_stop_words(string? text, bool stop)
    {
        Assert.Equal(stop, StopWords.IsStop(text));
    }

    [Fact]
    public void The_gate_ignores_the_playback_coming_back_and_passes_a_louder_voice()
    {
        var loud = Frames.Speech();
        var reference = Enumerable.Repeat(EchoGate.Rms(loud), 50).ToArray();
        var gate = new EchoGate(reference);

        Assert.False(gate.IsUserSpeech(loud, TimeSpan.FromSeconds(1)));
        Assert.False(gate.IsUserSpeech(Frames.Quiet(), TimeSpan.FromSeconds(1)));
        Assert.True(gate.IsUserSpeech(loud.Select(s => (short)(s * 2)).ToArray(), TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Past_the_end_of_the_playback_only_the_floor_counts()
    {
        var gate = EchoGate.FromWav(FakeSpeechEngine.Silence());

        Assert.True(gate.IsUserSpeech(Frames.Speech(), TimeSpan.FromSeconds(30)));
        Assert.False(gate.IsUserSpeech(Frames.Quiet(), TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task A_spoken_stop_is_reported_after_the_pause()
    {
        var engine = new FakeSpeechEngine("stop");
        var listener = new StopWordListener(engine.TranscribeAsync, () => TimeSpan.FromSeconds(1));
        listener.Begin(FakeSpeechEngine.Silence());

        var stopped = false;
        foreach (var frame in Frames.For(TimeSpan.FromMilliseconds(400), Frames.Speech).Concat(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet)))
        {
            stopped |= await listener.PushAsync(frame, CancellationToken.None);
        }

        Assert.True(stopped);
        Assert.Equal(1, engine.Transcribed);
    }

    [Fact]
    public async Task Other_words_and_quiet_do_not_interrupt()
    {
        var engine = new FakeSpeechEngine("dziękuję");
        var listener = new StopWordListener(engine.TranscribeAsync, () => TimeSpan.FromSeconds(1));
        listener.Begin(FakeSpeechEngine.Silence());

        var stopped = false;
        foreach (var frame in Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet).Concat(Frames.For(TimeSpan.FromMilliseconds(400), Frames.Speech)).Concat(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet)))
        {
            stopped |= await listener.PushAsync(frame, CancellationToken.None);
        }

        Assert.False(stopped);
        Assert.Equal(1, engine.Transcribed);
    }

    [Fact]
    public async Task Saying_stop_while_JARVIS_speaks_stops_the_sound_and_the_bridge_is_idle()
    {
        await using var bed = new BridgeTestBed("stop");
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();
        bed.Speaker.Block = true;

        await bed.Server.SendAsync("""{"type":"audio","format":"wav"}""");
        await bed.Server.SendBinaryAsync(FakeSpeechEngine.Silence());
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Speaking, "speaking");
        bed.Mic.Push(Frames.For(TimeSpan.FromMilliseconds(400), Frames.Speech));
        bed.Mic.Push(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet));

        await Wait.UntilAsync(() => bed.Speaker.Cancelled == 1, "the sound is cancelled");
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "idle after the interruption");
    }
}
