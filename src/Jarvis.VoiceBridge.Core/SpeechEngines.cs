namespace Jarvis.VoiceBridge;

/// <summary>Picks the speech engine for <c>--speech local</c>: an external one with <c>--engine</c>, else Whisper and Piper inside.</summary>
internal static class SpeechEngines
{
    /// <returns><c>null</c> when the server hears and speaks.</returns>
    public static ISpeechEngine? Create(BridgeOptions options, HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Engine is { } engine)
        {
            return new LocalSpeech(http, engine);
        }

        if (!options.SpeechLocal)
        {
            return null;
        }

        foreach (var file in new[] { options.SttModelFile, options.TtsModelFile, options.TtsModelFile + ".json" })
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(
                    $"Speech model missing: {file}. Download it as described in docs/bridge.md (\"Speech without Speaches\"), or set --stt-model / --tts-model.");
            }
        }

        return new InProcessSpeech(new WhisperTranscriber(options.SttModelFile), new PiperSynthesizer(options.TtsModelFile));
    }
}
