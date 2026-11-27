using System.Threading.Channels;

namespace Jarvis.VoiceBridge;

/// <summary>The microphone as the bridge sees it: 80 ms frames of 16 kHz mono PCM (<see cref="Microphone"/>).</summary>
internal interface IMicrophone
{
    string Name { get; }

    ChannelReader<short[]> Frames { get; }

    /// <summary>Drops what was captured meanwhile, e.g. the chime that just played.</summary>
    void Drain();
}
