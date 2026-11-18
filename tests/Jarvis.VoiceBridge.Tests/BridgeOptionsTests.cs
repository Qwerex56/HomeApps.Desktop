using Jarvis.VoiceBridge;

namespace Jarvis.VoiceBridge.Tests;

public sealed class BridgeOptionsTests
{
    [Fact]
    public void The_hologram_app_registers_as_JARVIS_on_the_computer()
    {
        var options = BridgeOptions.Parse(["--ipc", "--link", "--device", "mac-biuro"]);

        Assert.True(options.Link);
        Assert.Equal("JARVIS na komputerze (mac-biuro)", options.ClientName);
    }

    [Fact]
    public void The_terminal_bridge_keeps_its_name_and_links_only_when_asked()
    {
        var options = BridgeOptions.Parse(["--device", "mac-biuro"]);

        Assert.False(options.Link);
        Assert.Equal("JARVIS: most głosowy (mac-biuro)", options.ClientName);
    }
}
