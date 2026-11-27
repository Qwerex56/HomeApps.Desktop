namespace Jarvis.VoiceBridge;

/// <summary>The bridge's own ears and voice (S1): 16 kHz PCM in, text out; text in, WAV out. Fakes stand in for it in tests.</summary>
internal interface ISpeechEngine
{
    Task<string> TranscribeAsync(short[] pcm, CancellationToken cancellationToken);

    Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken);
}
