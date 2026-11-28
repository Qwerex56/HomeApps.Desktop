using System.Text.Json;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>D4 (requirement 2.10): a tool that is off is not announced to JARVIS and a call to it is refused on this computer.</summary>
public sealed class ExperimentalDeviceLinkTests
{
    [Fact]
    public async Task A_disabled_tool_is_not_announced_and_a_call_to_it_is_refused_here()
    {
        await using var server = new FakeVoiceServer();
        using var stop = new CancellationTokenSource();
        var enabled = new HashSet<string> { "now_playing" };
        var options = new BridgeOptions(server.Url, "test", 0.5f, false, null, null, false, Hosted: true);
        await using var link = new DeviceLink(options, new FakeAccessToken(), [], () => ExperimentalTools.Actions(enabled, "macos"));
        _ = link.RunAsync(stop.Token);
        try
        {
            await Wait.UntilAsync(() => server.Texts.Any(t => t.Contains("\"hello\"", StringComparison.Ordinal)), "hello");
            var hello = JsonDocument.Parse(server.Texts.First(t => t.Contains("\"hello\"", StringComparison.Ordinal)));
            var announced = hello.RootElement.GetProperty("actions").EnumerateArray().Select(a => a.GetProperty("name").GetString()!).ToArray();
            Assert.Equal(["now_playing"], announced);

            await server.SendAsync("""{"type":"action","id":"a1","name":"volume","input":{"level":5}}""");
            await Wait.UntilAsync(() => server.Texts.Any(t => t.Contains("\"action_result\"", StringComparison.Ordinal)), "action_result");

            var result = JsonDocument.Parse(server.Texts.First(t => t.Contains("\"action_result\"", StringComparison.Ordinal))).RootElement;
            Assert.False(result.GetProperty("ok").GetBoolean());
            Assert.Equal("Nie znam akcji volume.", result.GetProperty("text").GetString());
        }
        finally
        {
            await stop.CancelAsync();
        }
    }
}
