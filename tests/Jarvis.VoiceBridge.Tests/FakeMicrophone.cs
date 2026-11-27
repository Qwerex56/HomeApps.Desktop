using System.Threading.Channels;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>A microphone without hardware: the test pushes the frames.</summary>
internal sealed class FakeMicrophone : IMicrophone
{
    private readonly Channel<short[]> _frames = Channel.CreateUnbounded<short[]>();

    public string Name => "fake";

    public System.Threading.Channels.ChannelReader<short[]> Frames => _frames.Reader;

    public void Drain()
    {
    }

    public void Push(IEnumerable<short[]> frames)
    {
        foreach (var frame in frames)
        {
            _frames.Writer.TryWrite(frame);
        }
    }

    public void Push(short[] frame) => _frames.Writer.TryWrite(frame);
}
