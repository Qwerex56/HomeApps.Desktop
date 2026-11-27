namespace Jarvis.VoiceBridge;

/// <summary>"Hey Jarvis" detection on 80 ms frames (<see cref="WakeWord"/>).</summary>
internal interface IWakeWord
{
    void Reset();

    /// <returns>The wake word's score from 0 to 1 for this frame.</returns>
    float Process(short[] frame);
}
