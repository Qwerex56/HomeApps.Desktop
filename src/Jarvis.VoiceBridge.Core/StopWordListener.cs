namespace Jarvis.VoiceBridge;

/// <summary>
/// While JARVIS speaks the microphone is not ignored any more, but only a spoken "stop" or "cisza" counts: frames pass
/// the <see cref="EchoGate"/>, what remains is transcribed when the person pauses, and only a stop word is reported.
/// </summary>
internal sealed class StopWordListener(Func<short[], CancellationToken, Task<string>> transcribe, Func<TimeSpan> clock)
{
    private const int PauseFrames = 5;
    private const int MinimumSpeechFrames = 2;
    private const int LongestFrames = 31;

    private readonly Lock _lock = new();
    private readonly List<short> _heard = [];
    private EchoGate? _gate;
    private TimeSpan _started;
    private int _speechFrames;
    private int _quietFrames;

    public bool Active
    {
        get
        {
            lock (_lock)
            {
                return _gate is not null;
            }
        }
    }

    /// <summary>A sentence starts playing: <paramref name="wav"/> is the echo to ignore.</summary>
    public void Begin(byte[] wav)
    {
        var gate = EchoGate.FromWav(wav);
        lock (_lock)
        {
            _gate = gate;
            _started = clock();
            Reset();
        }
    }

    public void End()
    {
        lock (_lock)
        {
            _gate = null;
            Reset();
        }
    }

    /// <returns><c>true</c> when the person said a stop word.</returns>
    public async Task<bool> PushAsync(short[] frame, CancellationToken cancellationToken) =>
        Push(frame) is { } pcm && await IsStopAsync(pcm, cancellationToken);

    /// <summary>Hears the frame; returns the utterance once the person paused, to be checked off the audio loop with <see cref="IsStopAsync"/>.</summary>
    public short[]? Push(short[] frame)
    {
        short[] pcm;
        lock (_lock)
        {
            if (_gate is null)
            {
                return null;
            }

            if (_gate.IsUserSpeech(frame, clock() - _started))
            {
                _speechFrames++;
                _quietFrames = 0;
            }
            else if (_speechFrames == 0)
            {
                return null;
            }
            else
            {
                _quietFrames++;
            }

            _heard.AddRange(frame);
            if (_quietFrames < PauseFrames && _heard.Count < LongestFrames * WakeWord.FrameSamples)
            {
                return null;
            }

            // A click or a single loud frame is not an utterance.
            var enough = _speechFrames >= MinimumSpeechFrames;
            pcm = _heard.ToArray();
            Reset();
            if (!enough)
            {
                return null;
            }
        }

        return pcm;
    }

    public async Task<bool> IsStopAsync(short[] pcm, CancellationToken cancellationToken) =>
        StopWords.IsStop(await transcribe(pcm, cancellationToken));

    private void Reset()
    {
        _heard.Clear();
        _speechFrames = 0;
        _quietFrames = 0;
    }
}
