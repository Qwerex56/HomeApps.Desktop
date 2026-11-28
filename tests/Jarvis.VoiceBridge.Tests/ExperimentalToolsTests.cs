using System.Text.Json;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>The six experimental computer tools (requirement 2.10): hidden until on, the same set on macOS and Windows.</summary>
public sealed class ExperimentalToolsTests
{
    [Fact]
    public void Nothing_is_announced_by_default()
    {
        Assert.Empty(ExperimentalTools.Actions([], "macos"));
        Assert.Empty(ExperimentalTools.Actions([], "windows"));
    }

    [Fact]
    public void Windows_timer_has_no_double_quotes_in_the_child_command_and_volume_guards_low_levels()
    {
        var actions = ExperimentalTools.Actions(["timer", "volume"], "windows");
        Assert.DoesNotContain('"', actions.Single(a => a.Name == "timer").Run);
        Assert.Contains("if ($l -ge 2) { 1..[int]($l / 2)", actions.Single(a => a.Name == "volume").Run);
    }

    [Theory]
    [InlineData("macos", false)]
    [InlineData("windows", true)]
    public void All_six_tools_exist_on_both_systems(string system, bool powerShell)
    {
        var actions = ExperimentalTools.Actions(ExperimentalTools.Ids, system);

        Assert.Equal(["open_app", "open_url", "media_control", "now_playing", "volume", "timer"], actions.Select(a => a.Name));
        Assert.All(actions, a => Assert.Equal(powerShell, a.PowerShell));
        Assert.All(actions, a => Assert.False(string.IsNullOrWhiteSpace(a.Run)));
    }

    [Fact]
    public void Only_enabled_tools_are_announced_and_what_is_playing_is_read_only()
    {
        var actions = ExperimentalTools.Actions(["now_playing", "volume"], "macos");

        Assert.Equal(["now_playing", "volume"], actions.Select(a => a.Name));
        Assert.True(actions.Single(a => a.Name == "now_playing").ReadOnly);
        Assert.False(actions.Single(a => a.Name == "volume").ReadOnly);
    }

    [Fact]
    public void Other_systems_have_no_tools()
    {
        Assert.Empty(ExperimentalTools.Actions(ExperimentalTools.Ids, "linux"));
    }

    [Fact]
    public async Task The_mac_url_tool_refuses_anything_but_https()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
        {
            return;
        }

        var action = ExperimentalTools.Actions(["open_url"], "macos").Single();
        var input = JsonSerializer.SerializeToElement(new { url = "file:///etc/passwd" });

        var (ok, text) = await CommandRunner.RunAsync(action, input, CancellationToken.None);

        Assert.False(ok);
        Assert.Contains("https://", text);
    }

    [Fact]
    public async Task The_mac_volume_tool_refuses_a_level_outside_0_to_100()
    {
        if (!OperatingSystem.IsMacOS() && !OperatingSystem.IsLinux())
        {
            return;
        }

        var action = ExperimentalTools.Actions(["volume"], "macos").Single();

        var (ok, _) = await CommandRunner.RunAsync(action, JsonSerializer.SerializeToElement(new { level = "150; reboot" }), CancellationToken.None);

        Assert.False(ok);
    }
}
