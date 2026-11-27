namespace Jarvis.VoiceBridge;

/// <summary>Text to speech inside this process (<see cref="PiperSynthesizer"/>); returns a WAV.</summary>
internal interface ISynthesizer
{
    Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken);
}
