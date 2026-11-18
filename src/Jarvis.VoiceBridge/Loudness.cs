namespace Jarvis.VoiceBridge;

/// <summary>
/// The hologram's pulse (jarvis-satellites.md 4): JARVIS's answer cut into short windows, each one's loudness (RMS)
/// from 0 to 1 relative to the loudest window of that answer, so quiet and loud voices pulse alike.
/// </summary>
internal static class Loudness
{
    public static readonly TimeSpan Window = TimeSpan.FromMilliseconds(33);

    public static double[] Windows(byte[] wav)
    {
        var (samples, rate) = Wav.Read(wav);
        var size = Math.Max(1, (int)(rate * Window.TotalSeconds));
        var windows = new double[(samples.Length + size - 1) / size];
        for (var w = 0; w < windows.Length; w++)
        {
            var end = Math.Min(samples.Length, (w + 1) * size);
            double sum = 0;
            for (var i = w * size; i < end; i++)
            {
                sum += (double)samples[i] * samples[i];
            }

            windows[w] = Math.Sqrt(sum / Math.Max(1, end - (w * size)));
        }

        var loudest = windows.Length == 0 ? 0 : windows.Max();
        return loudest < 1 ? new double[windows.Length] : windows.Select(r => r / loudest).ToArray();
    }
}
