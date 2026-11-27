using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Jarvis.VoiceBridge;

/// <summary>
/// D10: text to speech with a Piper voice (<c>pl_PL-gosia-medium.onnx</c> and its <c>.onnx.json</c>) on ONNX Runtime, in
/// this process. Piper's model wants phoneme ids; the phonemes come from the <c>espeak-ng</c> program (what Piper
/// itself uses), so it has to be installed (docs/bridge.md "Speech without Speaches").
/// </summary>
internal sealed class PiperSynthesizer : ISynthesizer, IDisposable
{
    private readonly InferenceSession _session;
    private readonly Dictionary<string, long> _ids = [];
    private readonly string _voice;
    private readonly int _sampleRate;
    private readonly float[] _scales;
    private readonly SemaphoreSlim _one = new(1, 1);

    public PiperSynthesizer(string modelPath)
    {
        using var config = JsonDocument.Parse(File.ReadAllText(modelPath + ".json"));
        var root = config.RootElement;
        _sampleRate = root.GetProperty("audio").GetProperty("sample_rate").GetInt32();
        _voice = root.TryGetProperty("espeak", out var espeak) ? espeak.GetProperty("voice").GetString() ?? "pl" : "pl";
        var inference = root.GetProperty("inference");
        _scales =
        [
            inference.GetProperty("noise_scale").GetSingle(),
            inference.GetProperty("length_scale").GetSingle(),
            inference.GetProperty("noise_w").GetSingle(),
        ];
        foreach (var entry in root.GetProperty("phoneme_id_map").EnumerateObject())
        {
            _ids[entry.Name] = entry.Value[0].GetInt64();
        }

        _session = new InferenceSession(modelPath);
    }

    public async Task<byte[]> SynthesizeAsync(string text, CancellationToken cancellationToken)
    {
        var phonemes = await PhonemesAsync(text, cancellationToken);
        var ids = Encode(phonemes);
        await _one.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => Run(ids), cancellationToken);
        }
        finally
        {
            _one.Release();
        }
    }

    public void Dispose()
    {
        _session.Dispose();
        _one.Dispose();
    }

    /// <summary>Piper's ids: start, then every phoneme followed by a pad, then end.</summary>
    internal long[] Encode(string phonemes)
    {
        var ids = new List<long> { _ids["^"], _ids["_"] };
        foreach (var phoneme in phonemes.Normalize(NormalizationForm.FormD).EnumerateRunes())
        {
            if (_ids.TryGetValue(phoneme.ToString(), out var id))
            {
                ids.Add(id);
                ids.Add(_ids["_"]);
            }
        }

        ids.Add(_ids["$"]);
        return [.. ids];
    }

    private byte[] Run(long[] ids)
    {
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", new DenseTensor<long>(ids, [1, ids.Length])),
            NamedOnnxValue.CreateFromTensor("input_lengths", new DenseTensor<long>(new long[] { ids.Length }, [1])),
            NamedOnnxValue.CreateFromTensor("scales", new DenseTensor<float>(_scales, [3])),
        };
        using var results = _session.Run(inputs);
        var audio = results[0].AsEnumerable<float>().ToArray();
        var pcm = audio.Select(s => (short)Math.Clamp(s * short.MaxValue, short.MinValue, short.MaxValue)).ToArray();
        return Wav.Write(pcm, _sampleRate);
    }

    private async Task<string> PhonemesAsync(string text, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo("espeak-ng")
        {
            ArgumentList = { "-v", _voice, "-q", "--ipa", "--stdin" },
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        try
        {
            using var espeak = Process.Start(start) ?? throw new InvalidOperationException("Cannot start espeak-ng.");
            await espeak.StandardInput.WriteAsync(text.AsMemory(), cancellationToken);
            espeak.StandardInput.Close();
            var phonemes = await espeak.StandardOutput.ReadToEndAsync(cancellationToken);
            await espeak.WaitForExitAsync(cancellationToken);
            return string.Join(' ', phonemes.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new FileNotFoundException("espeak-ng is not installed (macOS: brew install espeak-ng).", exception);
        }
    }
}
