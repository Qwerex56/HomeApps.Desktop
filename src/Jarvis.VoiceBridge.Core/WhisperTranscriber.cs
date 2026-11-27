using System.Text;
using Whisper.net;

namespace Jarvis.VoiceBridge;

/// <summary>D10: speech to text with a GGML Whisper model (tiny or base) through Whisper.net, in this process.</summary>
internal sealed class WhisperTranscriber : ITranscriber, IDisposable
{
    private readonly WhisperFactory _factory;
    private readonly WhisperProcessor _processor;
    private readonly SemaphoreSlim _one = new(1, 1);

    public WhisperTranscriber(string modelPath)
    {
        _factory = WhisperFactory.FromPath(modelPath);
        _processor = _factory.CreateBuilder().WithLanguage("pl").Build();
    }

    public async Task<string> TranscribeAsync(short[] pcm, CancellationToken cancellationToken)
    {
        await _one.WaitAsync(cancellationToken);
        try
        {
            using var wav = new MemoryStream(Wav.Write(pcm, Microphone.SampleRate));
            var text = new StringBuilder();
            await foreach (var segment in _processor.ProcessAsync(wav, cancellationToken))
            {
                text.Append(segment.Text);
            }

            return text.ToString().Trim();
        }
        finally
        {
            _one.Release();
        }
    }

    public void Dispose()
    {
        _processor.Dispose();
        _factory.Dispose();
        _one.Dispose();
    }
}
