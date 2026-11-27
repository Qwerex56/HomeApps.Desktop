namespace Jarvis.VoiceBridge.Tests;

/// <summary>The echo gate's arithmetic on its edges: zero or missing reference, latency, clipping, empty frames.</summary>
public sealed class EchoGateTests
{
    private static short[] Constant(int value) => Enumerable.Repeat((short)value, WakeWord.FrameSamples).ToArray();

    [Fact]
    public void Rms_of_a_known_signal_and_of_nothing()
    {
        Assert.Equal(3.0, EchoGate.Rms(Constant(3)), 6);
        Assert.Equal(Math.Sqrt(12.5), EchoGate.Rms(new short[] { 3, -4 }), 6);
        Assert.Equal(0, EchoGate.Rms(ReadOnlySpan<short>.Empty));
    }

    [Fact]
    public void Zero_reference_leaves_only_the_floor()
    {
        var silent = new EchoGate(new double[10]);
        var none = new EchoGate([]);

        foreach (var gate in new[] { silent, none })
        {
            Assert.False(gate.IsUserSpeech(Constant(100), TimeSpan.FromSeconds(1)));
            Assert.True(gate.IsUserSpeech(Constant(600), TimeSpan.FromSeconds(1)));
        }
    }

    [Fact]
    public void The_floor_is_strict_and_a_quiet_echo_does_not_lower_it()
    {
        var gate = new EchoGate([100]);

        Assert.False(gate.IsUserSpeech(Constant(500), TimeSpan.FromSeconds(0.25)));
        Assert.True(gate.IsUserSpeech(Constant(501), TimeSpan.FromSeconds(0.25)));
    }

    [Fact]
    public void The_margin_over_the_echo_is_strict()
    {
        var gate = new EchoGate([1000]);
        var at = TimeSpan.FromSeconds(0.25);

        Assert.False(gate.IsUserSpeech(Constant(1500), at));
        Assert.True(gate.IsUserSpeech(Constant(1501), at));
    }

    [Fact]
    public void The_echo_is_looked_up_after_the_latency()
    {
        var reference = new double[30];
        reference[10] = 10_000;
        var gate = new EchoGate(reference);

        // 200 ms latency + 10 frames of 80 ms = the echo of frame 10 reaches the microphone.
        Assert.False(gate.IsUserSpeech(Constant(12_000), TimeSpan.FromMilliseconds(1000)));
        Assert.True(gate.IsUserSpeech(Constant(16_000), TimeSpan.FromMilliseconds(1000)));
    }

    [Fact]
    public void The_window_around_the_echo_reaches_one_frame_back_and_two_forward()
    {
        var reference = new double[30];
        reference[11] = 10_000;
        var gate = new EchoGate(reference);

        // Frame 9 looks at frames 8..11, so it hears the echo of frame 11; frame 8 looks at 7..10 and does not.
        Assert.False(gate.IsUserSpeech(Constant(12_000), TimeSpan.FromMilliseconds(920)));
        Assert.True(gate.IsUserSpeech(Constant(12_000), TimeSpan.FromMilliseconds(840)));
    }

    [Fact]
    public void Before_the_sound_reaches_the_microphone_nothing_is_subtracted()
    {
        // The echo of frame 5 is far away: the window at the start (frames 0..0 at 200 ms latency) sees none of it.
        var reference = new double[30];
        reference[5] = 10_000;
        var gate = new EchoGate(reference);

        Assert.True(gate.IsUserSpeech(Constant(12_000), TimeSpan.Zero));
        Assert.True(gate.IsUserSpeech(Constant(12_000), TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Clipped_full_scale_samples_neither_overflow_nor_pass_as_speech()
    {
        Assert.Equal(32_768, EchoGate.Rms(Constant(short.MinValue)), 6);

        var echoOfFullScale = new EchoGate([EchoGate.Rms(Constant(short.MaxValue))]);
        Assert.False(echoOfFullScale.IsUserSpeech(Constant(short.MinValue), TimeSpan.FromSeconds(0.25)));
        Assert.True(new EchoGate([0]).IsUserSpeech(Constant(short.MinValue), TimeSpan.FromSeconds(0.25)));
    }

    [Fact]
    public void An_empty_frame_is_never_speech_and_null_is_rejected()
    {
        var gate = new EchoGate([]);

        Assert.False(gate.IsUserSpeech([], TimeSpan.FromSeconds(1)));
        Assert.Throws<ArgumentNullException>(() => gate.IsUserSpeech(null!, TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void An_empty_or_silent_playback_is_a_zero_reference()
    {
        var empty = EchoGate.FromWav(Wav.Write([], Microphone.SampleRate));
        var silent = EchoGate.FromWav(FakeSpeechEngine.Silence());

        Assert.True(empty.IsUserSpeech(Constant(600), TimeSpan.FromSeconds(1)));
        Assert.True(silent.IsUserSpeech(Constant(600), TimeSpan.FromSeconds(1)));
        Assert.False(silent.IsUserSpeech(Constant(100), TimeSpan.FromSeconds(1)));
    }
}
