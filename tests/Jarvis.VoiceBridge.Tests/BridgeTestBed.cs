namespace Jarvis.VoiceBridge.Tests;

/// <summary>A real <see cref="Bridge"/> and <see cref="VoiceLink"/> over a loopback fake server, with fake microphone, wake word and speaker.</summary>
internal sealed class BridgeTestBed : IAsyncDisposable
{
    private readonly CancellationTokenSource _quit = new();
    private readonly VoiceLink _link;
    private readonly Bridge _bridge;
    private readonly Task _running;

    public BridgeTestBed(string heard = "stop", ISpeechEngine? linkSpeech = null)
    {
        Server = new FakeVoiceServer();
        var options = new BridgeOptions(Server.Url, "test", 0.5f, false, null, null, false, Hosted: true);
        _link = new VoiceLink(options, linkSpeech);
        Speech = new FakeSpeechEngine(heard);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        _bridge = new Bridge(
            options, Wake, new FakeAccessToken(), _link, Mic, Speaker, Output,
            new StopWordListener(Speech.TranscribeAsync, () => clock.Elapsed));
        _running = _bridge.RunAsync(_quit.Token);
    }

    public FakeVoiceServer Server { get; }

    public Bridge Bridge => _bridge;

    public FakeMicrophone Mic { get; } = new();

    public FakeWakeWord Wake { get; } = new();

    public FakeSpeaker Speaker { get; } = new();

    public FakeOutput Output { get; } = new();

    public FakeSpeechEngine Speech { get; }

    /// <summary>"Hey Jarvis", then a spoken sentence and the pause after it: the utterance is sent.</summary>
    public async Task SpeakToJarvisAsync()
    {
        await Wait.UntilAsync(() => Output.Last == BridgeStates.Idle, "connected");
        Wake.Say();
        Mic.Push(Frames.Quiet());
        await Wait.UntilAsync(() => Output.Last == BridgeStates.Listening, "listening after the wake word");
        Mic.Push(Frames.For(TimeSpan.FromSeconds(1), Frames.Speech));
        await Wait.UntilAsync(() => Server.Binary > 0, "audio reaches the server");
    }

    public async Task FinishUtteranceAsync()
    {
        Mic.Push(Frames.For(TimeSpan.FromSeconds(1), Frames.Quiet));
        await Wait.UntilAsync(() => Output.Last == BridgeStates.Thinking, "thinking after the pause");
    }

    public async ValueTask DisposeAsync()
    {
        await _quit.CancelAsync();
        try
        {
            await _running;
        }
        catch (OperationCanceledException)
        {
            // Quit.
        }

        _bridge.Dispose();
        await _link.DisposeAsync();
        await Server.DisposeAsync();
        _quit.Dispose();
    }
}
