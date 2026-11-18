using System.Text.Json;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>jarvis-satellites.md S-2b: the hologram app reads one JSON object per line.</summary>
public sealed class IpcOutputTests : IDisposable
{
    private readonly StringWriter _lines = new();
    private readonly IpcOutput _output;

    public IpcOutputTests() => _output = new IpcOutput(_lines);

    public void Dispose() => _lines.Dispose();

    private List<JsonElement> Sent() =>
        _lines.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line).RootElement).ToList();

    [Fact]
    public void Each_event_is_one_line_of_json_with_its_type()
    {
        _output.State(BridgeStates.Listening);
        _output.Heard("włącz światło");
        _output.Reply("Gotowe.", [new ReplyAction("done", "Światło w salonie")]);
        _output.Confirm();
        _output.Error("voice.engine_down", "Speaches nie odpowiada");
        _output.Login("https://app.localhost/connect");

        var sent = Sent();

        Assert.Equal(["state", "heard", "reply", "confirm", "error", "login"], sent.Select(e => e.GetProperty("type").GetString()));
        Assert.Equal("listening", sent[0].GetProperty("state").GetString());
        Assert.Equal("włącz światło", sent[1].GetProperty("text").GetString());
        Assert.Equal("Światło w salonie", sent[2].GetProperty("actions")[0].GetProperty("description").GetString());
        Assert.Equal("voice.engine_down", sent[4].GetProperty("code").GetString());
        Assert.Equal("https://app.localhost/connect", sent[5].GetProperty("url").GetString());
    }

    [Fact]
    public void A_state_is_sent_only_when_it_changes()
    {
        _output.State(BridgeStates.Idle);
        _output.State(BridgeStates.Idle);
        _output.State(BridgeStates.Listening);

        Assert.Equal(["idle", "listening"], Sent().Select(e => e.GetProperty("state").GetString()));
    }

    [Fact]
    public void Loudness_is_kept_between_0_and_1_with_two_decimals()
    {
        _output.Level(0.4567);
        _output.Level(3);

        Assert.Equal([0.46, 1.0], Sent().Select(e => e.GetProperty("level").GetDouble()));
    }
}
