namespace Jarvis.VoiceBridge;

/// <summary>Speech to text inside this process (<see cref="WhisperTranscriber"/>).</summary>
internal interface ITranscriber
{
    Task<string> TranscribeAsync(short[] pcm, CancellationToken cancellationToken);
}
