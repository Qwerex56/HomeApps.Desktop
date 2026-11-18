namespace Jarvis.VoiceBridge.Tests;

/// <summary>The hologram's pulse follows JARVIS's voice (jarvis-satellites.md 4).</summary>
public sealed class LoudnessTests
{
    [Fact]
    public void Each_window_is_relative_to_the_loudest_one()
    {
        const int rate = 16_000;
        var window = rate * 33 / 1000;
        var samples = new short[window * 3];
        Array.Fill(samples, (short)8000, window, window);
        Array.Fill(samples, (short)4000, window * 2, window);

        var levels = Loudness.Windows(Wav.Write(samples, rate));

        Assert.Equal(3, levels.Length);
        Assert.Equal(0, levels[0], 2);
        Assert.Equal(1, levels[1], 2);
        Assert.Equal(0.5, levels[2], 2);
    }

    [Fact]
    public void Silence_does_not_pulse() =>
        Assert.All(Loudness.Windows(Wav.Write(new short[16_000], 16_000)), level => Assert.Equal(0, level));
}
