namespace Jarvis.VoiceBridge;

/// <summary>Runs the bridge inside another app (the tray app) and reports its states; the only public surface of Core.</summary>
public sealed class VoiceBridgeHost(Action<string> onState, Action? onConfirm = null)
{
    private volatile JarvisApi? _api;

    private volatile Bridge? _bridge;
    private volatile bool _muted;
    private volatile DeviceLink? _device;
    private volatile IReadOnlyCollection<string> _experimental = [];

    /// <summary>Runs until <paramref name="cancellationToken"/> is cancelled. <paramref name="args"/> are the bridge's command-line options.</summary>
    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var options = BridgeOptions.Parse(args) with { Hosted = true };
        var output = new HostedOutput(onState, onConfirm);
        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, errors) => options.AcceptsCertificate(errors),
        });
        var store = new TokenStore(options.Server);
        var person = new OAuthLogin(http, options, store, output);
        var satellite = new SatelliteLogin(http, options, store);
        IAccessToken login = SatelliteLogin.IsPaired(store) ? satellite : person;
        _api = new JarvisApi(http, options.Server, person);

        using var wake = new WakeWord(await ModelFiles.EnsureAsync(http, cancellationToken));
        using var microphone = new Microphone();
        var speech = SpeechEngines.Create(options, http);
        using var disposeSpeech = speech as IDisposable;
        await using var link = new VoiceLink(options, speech);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var stopWords = speech is null ? null : new StopWordListener(speech.TranscribeAsync, () => clock.Elapsed);
        using var bridge = new Bridge(options, wake, login, link, microphone, new SystemSpeaker(), output, stopWords);
        await using var device = new DeviceLink(
            options, login, options.Actions is { } actions ? ComputerActions.Load(actions) : [], () => ExperimentalTools.Actions(_experimental));
        _device = device;
        var linked = device.RunAsync(cancellationToken);
        _bridge = bridge;
        try
        {
            if (_muted)
            {
                // Muted before the bridge was up.
                await bridge.SetMutedAsync(true, cancellationToken);
            }

            await bridge.RunAsync(cancellationToken);
        }
        finally
        {
            _bridge = null;
            _device = null;
            _api = null;
            try
            {
                await linked;
            }
            catch (OperationCanceledException)
            {
                // Quitting.
            }
        }
    }

    /// <summary>The REST API as the signed-in person while the bridge runs (history, confirmations); null before.</summary>
    internal JarvisApi? Api => _api;

    /// <summary>Mutes or unmutes the microphone; before the bridge runs, the choice is applied when it starts.</summary>
    public Task SetMutedAsync(bool mute, CancellationToken cancellationToken = default)
    {
        _muted = mute;
        return _bridge?.SetMutedAsync(mute, cancellationToken) ?? Task.CompletedTask;
    }

    /// <summary>The experimental computer tools (<see cref="ExperimentalTools.Ids"/>) JARVIS may see; all others stay hidden from him. Re-announced at once.</summary>
    public void SetExperimentalTools(IReadOnlyCollection<string> ids)
    {
        _experimental = ids.Where(id => ExperimentalTools.Ids.Contains(id)).ToArray();
        _device?.Reannounce();
    }

    /// <summary>The ids of the experimental tools, in the order the settings list them.</summary>
    public static IReadOnlyList<string> ExperimentalToolIds => ExperimentalTools.Ids;

    /// <summary>The settings label of an experimental tool.</summary>
    public static string ExperimentalToolLabel(string id) => ExperimentalTools.Label(id);
}
