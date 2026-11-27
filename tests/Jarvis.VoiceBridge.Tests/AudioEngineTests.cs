namespace Jarvis.VoiceBridge.Tests;

/// <summary>D10: the in-process engine is Whisper (hears) and Piper (speaks) behind seams; here with fakes, no model files.</summary>
public sealed class AudioEngineTests
{
    private sealed class FakeTranscriber(string text) : ITranscriber
    {
        public int Calls { get; private set; }

        public Task<string> TranscribeAsync(short[] pcm, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(text);
        }
    }

    private sealed class FakeSynthesizer : ISynthesizer
    {
        public string? Said { get; private set; }

        public Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken)
        {
            Said = text;
            return Task.FromResult(FakeSpeechEngine.Silence());
        }
    }

    [Fact]
    public async Task Speech_is_transcribed_in_process_and_trimmed()
    {
        var transcriber = new FakeTranscriber("  włącz światło \n");
        var engine = new InProcessSpeech(transcriber, new FakeSynthesizer());

        Assert.Equal("włącz światło", await engine.TranscribeAsync(Frames.Speech(), CancellationToken.None));
        Assert.Equal(1, transcriber.Calls);
    }

    [Fact]
    public async Task Nothing_heard_never_reaches_the_model()
    {
        var transcriber = new FakeTranscriber("x");
        var engine = new InProcessSpeech(transcriber, new FakeSynthesizer());

        Assert.Equal(string.Empty, await engine.TranscribeAsync([], CancellationToken.None));
        Assert.Equal(0, transcriber.Calls);
    }

    [Fact]
    public async Task Text_is_spoken_as_a_wav_in_process()
    {
        var synthesizer = new FakeSynthesizer();
        var engine = new InProcessSpeech(new FakeTranscriber(string.Empty), synthesizer);

        var wav = await engine.SpeakAsync("Dzień dobry.", CancellationToken.None);

        Assert.Equal("Dzień dobry.", synthesizer.Said);
        Assert.Equal(Microphone.SampleRate, Wav.Read(wav).SampleRate);
    }

    [Fact]
    public void Speech_local_without_an_engine_means_Whisper_and_Piper_inside_with_models_in_the_home_folder()
    {
        var options = BridgeOptions.Parse(["--speech", "local"]);

        Assert.True(options.LocalSpeech);
        Assert.Null(options.Engine);
        Assert.EndsWith("ggml-base.bin", options.SttModelFile, StringComparison.Ordinal);
        Assert.EndsWith("pl_PL-gosia-medium.onnx", options.TtsModelFile, StringComparison.Ordinal);
    }

    [Fact]
    public void An_engine_address_still_selects_the_external_engine_and_models_can_be_set()
    {
        var options = BridgeOptions.Parse(["--speech", "local", "--engine", "http://x:8000", "--stt-model", "/m/tiny.bin", "--tts-model", "/m/v.onnx"]);

        Assert.Equal(new Uri("http://x:8000/"), options.Engine);
        Assert.Equal("/m/tiny.bin", options.SttModelFile);
        Assert.Equal("/m/v.onnx", options.TtsModelFile);
        Assert.True(BridgeOptions.Parse([]).LocalSpeech);
        Assert.False(BridgeOptions.Parse(["--speech", "server"]).LocalSpeech);
    }

    [Fact]
    public void A_missing_model_is_reported_with_where_to_get_it()
    {
        var options = BridgeOptions.Parse(["--speech", "local", "--stt-model", "/nope/a.bin"]);

        using var http = new HttpClient();
        var failure = Assert.Throws<FileNotFoundException>(() => SpeechEngines.Create(options, http));
        Assert.Contains("docs/bridge.md", failure.Message, StringComparison.Ordinal);
    }
}
