using System.Diagnostics;

namespace Jarvis.VoiceBridge;

/// <summary>
/// Plays a WAV with the system's own player (<c>afplay</c> on macOS, <c>aplay</c> on Linux, PowerShell on Windows), so the bridge needs no
/// audio output code. Cancelling stops the sound at once.
/// </summary>
internal sealed class SystemSpeaker : ISpeaker
{
    /// <summary>The player for this system: <c>afplay</c>, PowerShell's <c>SoundPlayer</c> on Windows, else <c>aplay</c>.</summary>
    internal static ProcessStartInfo Command(string file, bool? windows = null, bool? mac = null)
    {
        if (mac ?? OperatingSystem.IsMacOS())
        {
            return new ProcessStartInfo("afplay") { ArgumentList = { file } };
        }

        if (windows ?? OperatingSystem.IsWindows())
        {
            var quoted = file.Replace("'", "''", StringComparison.Ordinal);
            return new ProcessStartInfo("powershell")
            {
                ArgumentList = { "-NoProfile", "-NonInteractive", "-Command", $"(New-Object System.Media.SoundPlayer '{quoted}').PlaySync()" },
                CreateNoWindow = true,
            };
        }

        return new ProcessStartInfo("aplay") { ArgumentList = { "-q", file } };
    }

    public async Task PlayAsync(byte[] wav, CancellationToken cancellationToken)
    {
        var file = Path.Combine(Path.GetTempPath(), $"jarvis-{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(file, wav, cancellationToken);
        var start = Command(file);
        try
        {
            using var player = Process.Start(start) ?? throw new InvalidOperationException($"Cannot start {start.FileName}.");
            try
            {
                await player.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                player.Kill();
                throw;
            }
        }
        finally
        {
            File.Delete(file);
        }
    }
}
