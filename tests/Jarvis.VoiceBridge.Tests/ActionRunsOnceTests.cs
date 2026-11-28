namespace Jarvis.VoiceBridge.Tests;

/// <summary>D16: a device action frame with the same id (redelivered after a reconnect) runs the command once.</summary>
public sealed class ActionRunsOnceTests
{
    [Fact]
    public async Task The_same_action_id_runs_the_command_once_and_is_answered_twice()
    {
        await using var server = new FakeVoiceServer();
        using var stop = new CancellationTokenSource();
        var marker = Path.Combine(Path.GetTempPath(), $"jarvis-once-{Guid.NewGuid():N}");
        var options = new BridgeOptions(server.Url, "test", 0.5f, false, null, null, false, Hosted: true);
        await using var link = new DeviceLink(options, new FakeAccessToken(), [new ComputerAction("mark", "Mark", $"echo x >> '{marker}'; echo ok", Confirm: false)]);
        _ = link.RunAsync(stop.Token);
        try
        {
            await Wait.UntilAsync(() => server.Texts.Any(t => t.Contains("\"hello\"", StringComparison.Ordinal)), "hello");
            await server.SendAsync("""{"type":"action","id":"a1","name":"mark"}""");
            await server.SendAsync("""{"type":"action","id":"a1","name":"mark"}""");
            await Wait.UntilAsync(() => server.Texts.Count(t => t.Contains("\"action_result\"", StringComparison.Ordinal)) == 2, "two results");

            Assert.Single(File.ReadAllLines(marker));
        }
        finally
        {
            await stop.CancelAsync();
            File.Delete(marker);
        }
    }
}
