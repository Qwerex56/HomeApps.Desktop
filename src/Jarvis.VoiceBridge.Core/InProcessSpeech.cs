namespace Jarvis.VoiceBridge;

/// <summary>D10: <c>--speech local</c> without Speaches: Whisper hears and Piper speaks, both inside this process.</summary>
internal sealed class InProcessSpeech(ITranscriber transcriber, ISynthesizer synthesizer) : ISpeechEngine, IDisposable
{
    public async Task<string> TranscribeAsync(short[] pcm, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pcm);
        return pcm.Length == 0 ? string.Empty : (await transcriber.TranscribeAsync(pcm, cancellationToken)).Trim();
    }

    public Task<byte[]> SpeakAsync(string text, CancellationToken cancellationToken) =>
        synthesizer.SynthesizeAsync(text, cancellationToken);

    public void Dispose()
    {
        (transcriber as IDisposable)?.Dispose();
        (synthesizer as IDisposable)?.Dispose();
    }
}
