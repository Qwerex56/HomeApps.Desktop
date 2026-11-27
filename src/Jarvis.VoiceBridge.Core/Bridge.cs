using System.Globalization;
using System.Net.WebSockets;
using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>
/// The bridge's loop (jarvis-voice-bridge.md 1): the wake word listens locally; after "Hey Jarvis" (or Enter) the
/// utterance streams to the server until the person is quiet; then the microphone is ignored while the server answers
/// and JARVIS speaks (only "stop" or "cisza" gets through, echo-cancelled). After EVERY answer the next utterance is
/// recorded without the wake word for <see cref="FollowUpWindow.Length"/> (D11).
/// </summary>
internal sealed class Bridge(
    BridgeOptions options, IWakeWord wake, IAccessToken login, VoiceLink link, IMicrophone microphone, ISpeaker speaker,
    IBridgeOutput output, StopWordListener? stopWords = null)
    : IDisposable
{
    private static readonly TimeSpan WaitAfterWake = TimeSpan.FromSeconds(5);

    /// <summary>The turn lock (ADR 2.5): one utterance at a time; the audio loop and every command take it.</summary>
    private readonly SemaphoreSlim _gate = new(1, 1);
    private BridgeState _state = BridgeState.Waiting;
    private EndOfSpeech _speech = new(WaitAfterWake);
    private List<short>? _recording;
    private bool _awaiting;
    private bool _resetWake;
    private bool _muted;
    private CancellationTokenSource _playing = new();
    private Task _playback = Task.CompletedTask;
    private Task _stopCheck = Task.CompletedTask;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var quit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await ConnectAsync(quit.Token);
        Console.WriteLine(options.Ipc
            ? $"Mikrofon: {microphone.Name}."
            : $"Mikrofon: {microphone.Name}. Powiedz „Hey Jarvis” albo naciśnij Enter; q kończy.");
        var events = EventsAsync(quit.Token);
        var keys = options.Hosted ? Task.CompletedTask : options.Ipc ? CommandsAsync(quit) : Task.Run(() => KeysAsync(quit), quit.Token);
        try
        {
            await foreach (var frame in microphone.Frames.ReadAllAsync(quit.Token))
            {
                await OnFrameAsync(frame, quit.Token);
            }
        }
        catch (OperationCanceledException) when (quit.IsCancellationRequested)
        {
            // q or Ctrl+C.
        }

        await quit.CancelAsync();
        await _playing.CancelAsync();
        await Task.WhenAny(_playback, Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None));
        await Task.WhenAll(events, keys).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    /// <summary>Mutes or unmutes the microphone (the tray app's hotkey and menu).</summary>
    public Task SetMutedAsync(bool mute, CancellationToken cancellationToken) =>
        UnderGateAsync(() => OnMuteAsync(mute, cancellationToken), cancellationToken);

    public void Dispose()
    {
        _gate.Dispose();
        _playing.Dispose();
    }

    private async Task OnFrameAsync(short[] frame, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_muted)
            {
                return;
            }

            switch (_state)
            {
                case BridgeState.Waiting:
                    if (_resetWake)
                    {
                        wake.Reset();
                        _resetWake = false;
                    }

                    var score = wake.Process(frame);
                    if (score >= options.Threshold)
                    {
                        Console.WriteLine($"» Hey Jarvis ({score:0.00})");
                        await BeginAsync(BridgeState.Recording, cancellationToken);
                    }

                    break;
                case BridgeState.Busy when stopWords is { Active: true }:
                    if (stopWords.Push(frame) is { } heard && _stopCheck.IsCompleted)
                    {
                        _stopCheck = CheckStopWordAsync(stopWords, heard, _playing.Token, cancellationToken);
                    }

                    break;
                case BridgeState.Recording or BridgeState.Answering or BridgeState.FollowUp:
                    await link.SendAudioAsync(frame, cancellationToken);
                    _recording?.AddRange(frame);
                    switch (_speech.Push(frame))
                    {
                        case SpeechState.Ended or SpeechState.TooLong:
                            await FinishAsync(cancellationToken);
                            break;
                        case SpeechState.NothingSaid:
                            await link.CancelAsync(cancellationToken);
                            Console.WriteLine(_state == BridgeState.Answering ? "(brak odpowiedzi, akcja dalej czeka na karcie)" : "(cisza)");
                            BackToWaiting();
                            break;
                    }

                    break;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Under the gate: the chime, then everything the microphone hears goes to the server.</summary>
    private async Task BeginAsync(BridgeState state, CancellationToken cancellationToken)
    {
        await StopPlayingAsync();
        _state = state;
        _speech = new EndOfSpeech(state is BridgeState.Answering or BridgeState.FollowUp ? FollowUpWindow.Length : WaitAfterWake);
        _recording = options.SaveRecordings is null ? null : [];
        output.State(BridgeStates.Listening);
        await speaker.PlayAsync(Chime.Listening, cancellationToken);
        microphone.Drain();
        await link.StartAsync(cancellationToken);
    }

    /// <summary>Under the gate: the utterance is complete, the server takes over.</summary>
    private async Task FinishAsync(CancellationToken cancellationToken)
    {
        await link.StopAsync(cancellationToken);
        _state = BridgeState.Busy;
        output.State(BridgeStates.Thinking);
        _ = speaker.PlayAsync(Chime.Heard, cancellationToken);
        if (_recording is { Count: > 0 } recording && options.SaveRecordings is { } folder)
        {
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, $"{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}.wav");
            await File.WriteAllBytesAsync(file, Wav.Write(recording.ToArray(), Microphone.SampleRate), cancellationToken);
        }

        _recording = null;
    }

    private void BackToWaiting()
    {
        _state = BridgeState.Waiting;
        _awaiting = false;
        _resetWake = true;
        output.State(_muted ? BridgeStates.Muted : BridgeStates.Idle);
    }

    private async Task EventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var said in link.Events.ReadAllAsync(cancellationToken))
            {
                await OnEventAsync(said, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Quitting.
        }
    }

    private async Task OnEventAsync(ServerEvent said, CancellationToken cancellationToken)
    {
        switch (said.Type)
        {
            case "transcript":
                output.Heard(said.Text("text"));
                break;
            case "reply":
                output.Reply(
                    said.Text("text") ?? string.Empty,
                    said.Json.TryGetProperty("actions", out var actions)
                        ? actions.EnumerateArray()
                            .Select(a => new ReplyAction(
                                a.GetProperty("status").GetString() ?? string.Empty,
                                a.GetProperty("description").GetString() ?? string.Empty))
                            .ToList()
                        : []);
                _awaiting = said.Flag("awaiting");
                break;
            case "audio" when said.Wav is { } wav:
                // Nothing is played once the turn ended (stopped, lease_conflict).
                // Played on its own task (sentences in order), so later events (lease_conflict, done) are not held up.
                await UnderGateAsync(() =>
                {
                    if (_state == BridgeState.Busy)
                    {
                        var token = _playing.Token;
                        var before = _playback;
                        _playback = Task.Run(async () =>
                        {
                            await before.ContinueWith(_ => { }, TaskScheduler.Default);
                            await SpeakAsync(wav, token, cancellationToken);
                        }, CancellationToken.None);
                    }

                    return true;
                }, cancellationToken);

                break;
            case "lease_conflict":
                // Q2(A): another computer holds the turn: go silent and idle at once, send nothing, play nothing.
                await UnderGateAsync(() => SilenceAsync(), cancellationToken);
                break;
            case "done":
                // After the sentences still playing; the events loop itself stays free for lease_conflict and interrupts.
                await UnderGateAsync(() =>
                {
                    var token = _playing.Token;
                    var before = _playback;
                    _playback = Task.Run(async () =>
                    {
                        await before.ContinueWith(_ => { }, TaskScheduler.Default);
                        if (!token.IsCancellationRequested)
                        {
                            await UnderGateAsync(() => OnDoneAsync(said, token, cancellationToken), cancellationToken);
                        }
                    }, CancellationToken.None).ContinueWith(_ => { }, TaskScheduler.Default);
                    return true;
                }, cancellationToken);
                break;
            case "error":
                output.Error(said.Text("code"), said.Text("detail"));
                await UnderGateAsync(() => { BackToWaiting(); return Task.CompletedTask; }, cancellationToken);
                break;
            case "closed" when !cancellationToken.IsCancellationRequested:
                Console.WriteLine("Połączenie z serwerem zerwane, łączę ponownie…");
                await UnderGateAsync(() => { BackToWaiting(); return Task.CompletedTask; }, cancellationToken);
                await ConnectAsync(cancellationToken);
                break;
        }
    }

    /// <summary>Under the gate: the answer is spoken; the next utterance is expected (D11) unless the turn stopped meanwhile.</summary>
    private async Task OnDoneAsync(ServerEvent said, CancellationToken playing, CancellationToken cancellationToken)
    {
        if (_state != BridgeState.Busy || playing.IsCancellationRequested)
        {
            return;
        }

        // The turn is over: a slow stop-word check of it must not touch the next one.
        await StopPlayingAsync();
        var cancelled = said.Flag("cancelled");
        if (_awaiting && !cancelled)
        {
            output.Confirm();
            await BeginAsync(BridgeState.Answering, cancellationToken);
        }
        else if (FollowUpWindow.Opens(cancelled, _muted))
        {
            await BeginAsync(BridgeState.FollowUp, cancellationToken);
        }
        else
        {
            BackToWaiting();
        }
    }

    private void KeysAsync(CancellationTokenSource quit)
    {
        if (Console.IsInputRedirected)
        {
            return;
        }

        while (!quit.IsCancellationRequested)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.KeyChar is 'q' or 'Q')
            {
                quit.Cancel();
                return;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                UnderGateAsync(() => OnEnterAsync(quit.Token), quit.Token).GetAwaiter().GetResult();
            }
        }
    }

    /// <summary>Enter: speak without the wake word, end the utterance now, or stop JARVIS mid-answer.</summary>
    private async Task OnEnterAsync(CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case BridgeState.Waiting:
                Console.WriteLine("» Enter");
                await BeginAsync(BridgeState.Recording, cancellationToken);
                break;
            case BridgeState.Recording or BridgeState.Answering or BridgeState.FollowUp:
                await FinishAsync(cancellationToken);
                break;
            case BridgeState.Busy:
                await InterruptAsync(cancellationToken);
                break;
        }
    }

    /// <summary>Off the gate (the transcription is slow): a stop word interrupts only if this playback is still the current one.</summary>
    private async Task CheckStopWordAsync(StopWordListener listener, short[] heard, CancellationToken playing, CancellationToken cancellationToken)
    {
        try
        {
            if (await listener.IsStopAsync(heard, playing))
            {
                await UnderGateAsync(async () =>
                {
                    if (_state == BridgeState.Busy && !playing.IsCancellationRequested && playing == _playing.Token)
                    {
                        await InterruptAsync(cancellationToken);
                    }
                }, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)
        {
            // Interrupted or quitting.
        }
        catch (Exception exception)
        {
            output.Error("voice.engine_failed", exception.Message);
        }
    }

    /// <summary>Under the gate: JARVIS stops speaking and the server drops the turn.</summary>
    private async Task InterruptAsync(CancellationToken cancellationToken)
    {
        await StopPlayingAsync();
        await link.CancelAsync(cancellationToken);
        Console.WriteLine("(przerwane)");
        BackToWaiting();
    }

    /// <summary>Under the gate: the server gave the turn to another computer; nothing is sent back, nothing is played.</summary>
    private async Task SilenceAsync()
    {
        await StopPlayingAsync();
        link.Drop();
        _recording = null;
        Console.WriteLine("(inny komputer odpowiada)");
        BackToWaiting();
    }

    private async Task StopPlayingAsync()
    {
        stopWords?.End();
        await _playing.CancelAsync();

        // Not disposed: the playback task may still hold its token.
        _playing = new CancellationTokenSource();
    }

    /// <summary>Plays one sentence of the answer; with <c>--ipc</c> its loudness goes out for the hologram's pulse.</summary>
    private async Task SpeakAsync(byte[] wav, CancellationToken playing, CancellationToken cancellationToken)
    {
        if (playing.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!await TryUnderGateAsync(() =>
            {
                if (_state != BridgeState.Busy || playing.IsCancellationRequested)
                {
                    return false;
                }

                output.State(BridgeStates.Speaking);
                return true;
            }))
        {
            return;
        }

        using var pulse = CancellationTokenSource.CreateLinkedTokenSource(playing);
        var levels = options.Ipc ? PulseAsync(Loudness.Windows(wav), pulse.Token) : Task.CompletedTask;
        stopWords?.Begin(wav);
        try
        {
            await speaker.PlayAsync(wav, playing);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Enter or "stop" stopped him.
        }
        finally
        {
            stopWords?.End();
            await pulse.CancelAsync();
            await levels;
            output.Level(0);
            await TryUnderGateAsync(() =>
            {
                if (_state == BridgeState.Busy && !playing.IsCancellationRequested)
                {
                    output.State(BridgeStates.Thinking);
                }

                return true;
            });
        }
    }

    private async Task<bool> TryUnderGateAsync(Func<bool> action)
    {
        try
        {
            return await UnderGateAsync(action, CancellationToken.None);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private async Task PulseAsync(double[] levels, CancellationToken cancellationToken)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            while (clock.Elapsed / Loudness.Window is var at && at < levels.Length)
            {
                output.Level(levels[(int)at]);
                await Task.Delay(Loudness.Window, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // He stopped speaking.
        }
    }

    /// <summary><c>--ipc</c>: commands from the hologram app, one JSON line each; stdin closing means quit.</summary>
    private async Task CommandsAsync(CancellationTokenSource quit)
    {
        try
        {
            while (!quit.IsCancellationRequested && await Console.In.ReadLineAsync(quit.Token) is { } line)
            {
                var command = IpcCommands.Parse(line);
                switch (command)
                {
                    case IpcCommand.Quit:
                        await quit.CancelAsync();
                        return;
                    case IpcCommand.Talk:
                        await UnderGateAsync(() => OnEnterAsync(quit.Token), quit.Token);
                        break;
                    case IpcCommand.Stop:
                        await UnderGateAsync(() => OnStopAsync(quit.Token), quit.Token);
                        break;
                    case IpcCommand.Mute or IpcCommand.Unmute:
                        await UnderGateAsync(() => OnMuteAsync(command == IpcCommand.Mute, quit.Token), quit.Token);
                        break;
                    default:
                        output.Error("ipc.unknown_command", line);
                        break;
                }
            }

            await quit.CancelAsync();
        }
        catch (OperationCanceledException)
        {
            // Quitting.
        }
    }

    /// <summary>"stop": drop what is being recorded, or stop JARVIS mid-answer.</summary>
    private async Task OnStopAsync(CancellationToken cancellationToken)
    {
        switch (_state)
        {
            case BridgeState.Recording or BridgeState.Answering or BridgeState.FollowUp:
                await link.CancelAsync(cancellationToken);
                _recording = null;
                BackToWaiting();
                break;
            case BridgeState.Busy:
                await InterruptAsync(cancellationToken);
                break;
        }
    }

    private async Task OnMuteAsync(bool mute, CancellationToken cancellationToken)
    {
        _muted = mute;
        if (mute && _state is BridgeState.Recording or BridgeState.Answering or BridgeState.FollowUp)
        {
            await link.CancelAsync(cancellationToken);
            _recording = null;
            BackToWaiting();
        }
        else if (_state == BridgeState.Waiting)
        {
            BackToWaiting();
        }
    }

    private async Task<T> UnderGateAsync<T>(Func<T> read, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return read();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task UnderGateAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await action();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        output.State(BridgeStates.Connecting);
        while (true)
        {
            try
            {
                await link.ConnectAsync(await login.AccessTokenAsync(cancellationToken), cancellationToken);
                Console.WriteLine($"Połączono z {options.Server.Host}.");
                output.State(_muted ? BridgeStates.Muted : BridgeStates.Idle);
                return;
            }
            catch (Exception exception) when (exception is HttpRequestException or WebSocketException or JsonException)
            {
                Console.WriteLine($"Nie mogę połączyć się z {options.Server.Host} ({exception.Message}); próbuję za 5 s.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
