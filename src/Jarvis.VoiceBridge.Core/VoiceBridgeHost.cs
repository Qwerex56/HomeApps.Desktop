namespace Jarvis.VoiceBridge;

/// <summary>Runs the bridge inside another app (the tray app) and reports its states; the only public surface of Core.</summary>
public sealed class VoiceBridgeHost(Action<string> onState)
{
    private volatile Bridge? _bridge;

    /// <summary>Runs until <paramref name="cancellationToken"/> is cancelled. <paramref name="args"/> are the bridge's command-line options.</summary>
    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var options = BridgeOptions.Parse(args) with { Hosted = true };
        var output = new HostedOutput(onState);
        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, errors) => options.AcceptsCertificate(errors),
        });
        var store = new TokenStore(options.Server);
        var person = new OAuthLogin(http, options, store, output);
        var satellite = new SatelliteLogin(http, options, store);
        IAccessToken login = SatelliteLogin.IsPaired(store) ? satellite : person;

        using var wake = new WakeWord(await ModelFiles.EnsureAsync(http, cancellationToken));
        using var microphone = new Microphone();
        await using var link = new VoiceLink(options, options.Engine is { } engine ? new LocalSpeech(http, engine) : null);
        using var bridge = new Bridge(options, wake, login, link, microphone, output);
        await using var device = options.Actions is not null || options.Link || login is SatelliteLogin
            ? new DeviceLink(options, login, options.Actions is { } actions ? ComputerActions.Load(actions) : [])
            : null;
        var linked = device?.RunAsync(cancellationToken) ?? Task.CompletedTask;
        _bridge = bridge;
        try
        {
            await bridge.RunAsync(cancellationToken);
        }
        finally
        {
            _bridge = null;
        }

        await linked;
    }

    /// <summary>Mutes or unmutes the microphone; does nothing while the bridge is not running.</summary>
    public Task SetMutedAsync(bool mute, CancellationToken cancellationToken = default) =>
        _bridge?.SetMutedAsync(mute, cancellationToken) ?? Task.CompletedTask;
}
