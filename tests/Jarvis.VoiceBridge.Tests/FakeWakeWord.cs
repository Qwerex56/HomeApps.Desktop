namespace Jarvis.VoiceBridge.Tests;

/// <summary>Hears "Hey Jarvis" on the next frame after <see cref="Say"/>.</summary>
internal sealed class FakeWakeWord : IWakeWord
{
    private volatile bool _said;

    public void Say() => _said = true;

    public void Reset()
    {
    }

    public float Process(short[] frame)
    {
        var score = _said ? 1f : 0f;
        _said = false;
        return score;
    }
}
