namespace Jarvis.VoiceBridge.Tests;

/// <summary>An in-process engine whose model fails (missing file, ONNX or Whisper error).</summary>
internal sealed class ThrowingSpeechEngine(bool hears) : ISpeechEngine
{
    public Task<string> TranscribeAsync(short[] pcm, CancellationToken cancellationToken) =>
        hears ? Task.FromResult("włącz światło") : throw new InvalidOperationException("whisper failed");

    public Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken) =>
        throw new FileNotFoundException("voice model missing");
}
