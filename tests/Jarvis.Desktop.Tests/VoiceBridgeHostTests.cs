using Jarvis.VoiceBridge;

namespace Jarvis.Desktop.Tests;

public class VoiceBridgeHostTests
{
    [Fact]
    public async Task SetMutedAsync_BeforeTheBridgeRuns_ReportsNoState()
    {
        var states = new List<string>();
        var host = new VoiceBridgeHost(states.Add);

        await host.SetMutedAsync(true);
        await host.SetMutedAsync(false);

        Assert.Empty(states);
    }
}
