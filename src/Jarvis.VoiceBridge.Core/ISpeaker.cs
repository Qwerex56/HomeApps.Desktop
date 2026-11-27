namespace Jarvis.VoiceBridge;

/// <summary>Plays a WAV and returns when it ended; cancelling stops the sound at once (<see cref="SystemSpeaker"/>).</summary>
internal interface ISpeaker
{
    Task PlayAsync(byte[] wav, CancellationToken cancellationToken);
}
