namespace Jarvis.VoiceBridge;

/// <summary>
/// The echo canceller on the playback buffer (D10/D11): the WAV that is being played is the reference. A microphone
/// frame counts as the person's voice only when it is clearly louder than the reference at that moment (the speaker's
/// own sound coming back), so JARVIS does not take his voice for an interruption. A level gate against the known
/// playback, not an adaptive filter: no new package, and the only thing it has to find is a spoken "stop".
/// </summary>
internal sealed class EchoGate
{
    /// <summary>How much louder than the echo the microphone must be.</summary>
    public const double Margin = 1.5;

    /// <summary>Quieter than this is never speech (same as the end-of-speech detector).</summary>
    public const double Floor = 500;

    /// <summary>The player starts a moment after we do and the sound takes time to reach the microphone.</summary>
    public static readonly TimeSpan Latency = TimeSpan.FromMilliseconds(200);

    private const double FrameMs = 1000.0 * WakeWord.FrameSamples / 16_000;

    private readonly double[] _reference;

    public EchoGate(double[] reference) => _reference = reference;

    /// <summary>The RMS of every 80 ms of the playback, i.e. one value per microphone frame.</summary>
    public static EchoGate FromWav(byte[] wav)
    {
        var (samples, rate) = Wav.Read(wav);
        var size = Math.Max(1, (int)(rate * FrameMs / 1000));
        var levels = new double[(samples.Length + size - 1) / size];
        for (var f = 0; f < levels.Length; f++)
        {
            levels[f] = Rms(samples.AsSpan(f * size, Math.Min(size, samples.Length - (f * size))));
        }

        return new EchoGate(levels);
    }

    public static double Rms(ReadOnlySpan<short> frame)
    {
        double sum = 0;
        foreach (var sample in frame)
        {
            sum += (double)sample * sample;
        }

        return Math.Sqrt(sum / Math.Max(1, frame.Length));
    }

    /// <param name="frame">What the microphone heard.</param>
    /// <param name="playedFor">How long the playback has been going.</param>
    public bool IsUserSpeech(short[] frame, TimeSpan playedFor)
    {
        ArgumentNullException.ThrowIfNull(frame);
        var at = (int)((playedFor - Latency).TotalMilliseconds / FrameMs);
        double echo = 0;
        for (var i = Math.Max(0, at - 1); i <= at + 2 && i < _reference.Length; i++)
        {
            echo = Math.Max(echo, _reference[i]);
        }

        return Rms(frame) > Math.Max(Floor, echo * Margin);
    }
}
