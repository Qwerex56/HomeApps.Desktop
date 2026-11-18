namespace Jarvis.VoiceBridge;

/// <summary>What the hologram app can ask of the bridge over stdin (<c>{"type":"talk"}</c> and so on).</summary>
internal enum IpcCommand
{
    /// <summary>Like Enter: start without the wake word, or end the utterance now.</summary>
    Talk,

    /// <summary>Drop the utterance being recorded, or stop JARVIS mid-answer.</summary>
    Stop,

    /// <summary>The microphone is ignored, even the wake word, until <see cref="Unmute"/>.</summary>
    Mute,

    Unmute,

    Quit,
}
