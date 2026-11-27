using Jarvis.VoiceBridge;

// JARVIS's ears and mouth on the Mac (jarvis-voice-bridge.md, docs/jarvis.md "Voice bridge").
//   dotnet run --project src/Jarvis.VoiceBridge -- [--server https://home-apps.dev] [--device mac-biuro]
//     [--threshold 0.5] [--insecure] [--save-recordings ~/jarvis-nagrania] [--check-wake plik.wav] [--logout]
//     [--speech server | [--engine http://localhost:8000] [--stt-model ggml.bin] [--tts-model voice.onnx]] [--actions akcje.json] [--pair ABCD-EFGH] [--unpair] [--ipc] [--link]
var options = BridgeOptions.Parse(args);

// --ipc (S-2b): stdout carries only the JSON events for the hologram app; everything else people read goes to stderr.
IBridgeOutput output = new TerminalOutput();
if (options.Ipc)
{
    var events = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
    Console.SetOut(Console.Error);
    output = new IpcOutput(events);
}

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.Cancel();
};

using var http = new HttpClient(new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (_, _, _, errors) => options.AcceptsCertificate(errors),
});
var store = new TokenStore(options.Server);
var person = new OAuthLogin(http, options, store, output);
var satellite = new SatelliteLogin(http, options, store);
if (options.Logout)
{
    person.Forget();
    satellite.Forget();
    Console.WriteLine("Zapomniałem logowanie i parowanie; odłącz też aplikację w „Połączonych aplikacjach”.");
    return;
}

if (options.Unpair)
{
    satellite.Forget();
    Console.WriteLine("Zapomniałem parowanie z domem; most znów działa jako Ty. Usuń też satelitę na stronie Urządzenia.");
    return;
}

if (options.Pair is { } code)
{
    try
    {
        var paired = await satellite.PairAsync(code, stop.Token);
        Console.WriteLine($"Sparowano: ten komputer to satelita „{paired.Name}” swojego domu.");
    }
    catch (InvalidOperationException exception)
    {
        Console.Error.WriteLine(exception.Message);
        return;
    }
    catch (HttpRequestException exception)
    {
        Console.Error.WriteLine($"Nie mogę połączyć się z {options.Server}: {exception.Message}");
        return;
    }
}

// Paired once, the bridge is the household's satellite (S-1e): it speaks as the household and is always on its link.
IAccessToken login = SatelliteLogin.IsPaired(store) ? satellite : person;

using var wake = new WakeWord(await ModelFiles.EnsureAsync(http, stop.Token));
if (options.CheckWake is { } file)
{
    var (samples, rate) = Wav.Read(await File.ReadAllBytesAsync(file, stop.Token));
    Console.WriteLine(rate == Microphone.SampleRate
        ? $"Najwyższy wynik „Hey Jarvis”: {wake.Scan(samples):0.000} (próg {options.Threshold})."
        : $"Plik musi mieć 16 kHz, ma {rate} Hz.");
    return;
}

using var microphone = new Microphone();
var speech = SpeechEngines.Create(options, http);
using var disposeSpeech = speech as IDisposable;
await using var link = new VoiceLink(options, speech);
var clock = System.Diagnostics.Stopwatch.StartNew();
var stopWords = speech is null ? null : new StopWordListener(speech.TranscribeAsync, () => clock.Elapsed);
using var bridge = new Bridge(options, wake, login, link, microphone, new SystemSpeaker(), output, stopWords);
await using var device = options.Actions is not null || options.Link || login is SatelliteLogin
    ? new DeviceLink(options, login, options.Actions is { } actions ? ComputerActions.Load(actions) : [])
    : null;
var linked = device?.RunAsync(stop.Token) ?? Task.CompletedTask;
await bridge.RunAsync(stop.Token);
await stop.CancelAsync();
await linked;
