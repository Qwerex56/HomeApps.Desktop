namespace Jarvis.VoiceBridge.Tests;

/// <summary>Hears a fixed text and speaks silence; no model, no microphone.</summary>
internal sealed class FakeSpeechEngine(string heard) : ISpeechEngine
{
    public int Transcribed { get; private set; }

    public Task<string> TranscribeAsync(short[] pcm, CancellationToken cancellationToken)
    {
        Transcribed++;
        return Task.FromResult(heard);
    }

    public Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken) => Task.FromResult(Silence());

    public static byte[] Silence() => Wav.Write(new short[Microphone.SampleRate * 2], Microphone.SampleRate);
}
