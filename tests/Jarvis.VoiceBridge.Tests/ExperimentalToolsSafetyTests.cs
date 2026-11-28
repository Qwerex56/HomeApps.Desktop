using System.Text.Json;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>D4 (requirement 2.10, ADR Q3): the six tools' confirmations, input fields and command construction on macOS and Windows.</summary>
public sealed class ExperimentalToolsSafetyTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    private static readonly string[] Forbidden = ["clipboard", "screenshot", "shortcut", "hotkey", "keys"];

    // The input field each tool takes; now_playing takes none.
    private static readonly Dictionary<string, string[]> Fields = new()
    {
        ["open_app"] = ["APP"],
        ["open_url"] = ["URL"],
        ["media_control"] = ["COMMAND"],
        ["now_playing"] = [],
        ["volume"] = ["LEVEL"],
        ["timer"] = ["MINUTES", "TEXT"],
    };

    [Fact]
    public void The_set_is_exactly_six_and_has_no_clipboard_screenshot_or_shortcut_tool()
    {
        Assert.Equal(6, ExperimentalTools.Ids.Count);
        Assert.Equal(6, VoiceBridgeHost.ExperimentalToolIds.Count);
        Assert.All(ExperimentalTools.Ids, id => Assert.DoesNotContain(id, Forbidden));
        Assert.All(ExperimentalTools.Ids, id => Assert.False(string.IsNullOrWhiteSpace(VoiceBridgeHost.ExperimentalToolLabel(id))));
    }

    [Theory]
    [InlineData("macos")]
    [InlineData("windows")]
    public void Every_tool_except_what_plays_asks_for_confirmation(string system)
    {
        var actions = ExperimentalTools.Actions(ExperimentalTools.Ids, system);

        foreach (var action in actions)
        {
            Assert.Equal(action.Name != "now_playing", action.Confirm);
        }

        Assert.True(actions.Single(a => a.Name == "now_playing").ReadOnly);
        Assert.False(actions.Single(a => a.Name == "now_playing").Confirm);
    }

    [Theory]
    [InlineData("macos")]
    [InlineData("windows")]
    public void Each_tool_declares_exactly_its_input_fields(string system)
    {
        foreach (var action in ExperimentalTools.Actions(ExperimentalTools.Ids, system))
        {
            var expected = Fields[action.Name].Select(f => f.ToLowerInvariant()).Order().ToArray();
            if (action.Input is not { } input)
            {
                Assert.Empty(expected);
                continue;
            }

            var properties = input.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order().ToArray();
            Assert.Equal(expected, properties);
            Assert.Equal("object", input.GetProperty("type").GetString());
        }
    }

    [Theory]
    [InlineData("macos")]
    [InlineData("windows")]
    public void Input_is_read_only_from_environment_variables_in_the_command(string system)
    {
        // The command text never contains the input itself: each field is only referenced as an env variable, quoted for sh.
        foreach (var action in ExperimentalTools.Actions(ExperimentalTools.Ids, system))
        {
            foreach (var field in Fields[action.Name])
            {
                var reference = system == "windows" ? $"$env:JARVIS_{field}" : $"$JARVIS_{field}";
                var braced = $"${{JARVIS_{field}";
                Assert.True(action.Run.Contains(reference) || action.Run.Contains(braced), $"{action.Name} does not read {field} from the environment");
            }
        }
    }

    [Fact]
    public void Windows_commands_run_in_powershell_and_macos_commands_in_sh()
    {
        Assert.All(ExperimentalTools.Actions(ExperimentalTools.Ids, "windows"), a => Assert.True(a.PowerShell));
        Assert.All(ExperimentalTools.Actions(ExperimentalTools.Ids, "macos"), a => Assert.False(a.PowerShell));
    }

    [Fact]
    public void Windows_volume_and_timer_check_their_numbers_before_acting()
    {
        var volume = ExperimentalTools.Actions(["volume"], "windows").Single().Run;
        var timer = ExperimentalTools.Actions(["timer"], "windows").Single().Run;

        Assert.Contains("TryParse($env:JARVIS_LEVEL", volume);
        Assert.Contains("-gt 100", volume);
        Assert.Contains("TryParse($env:JARVIS_MINUTES", timer);
        Assert.Contains("-gt 1440", timer);
    }

    [Fact]
    public void Macos_volume_and_timer_check_their_numbers_before_acting()
    {
        var volume = ExperimentalTools.Actions(["volume"], "macos").Single().Run;
        var timer = ExperimentalTools.Actions(["timer"], "macos").Single().Run;

        Assert.Contains("[!0-9]*)", volume);
        Assert.Contains("-le 100", volume);
        Assert.Contains("[!0-9]*|0*)", timer);
        Assert.Contains("-le 1440", timer);
    }

    [Fact]
    public async Task On_macos_an_injected_app_name_is_one_argument_and_runs_nothing_extra()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var marker = Path.Combine(_dir, "pwned-app");
        var action = ExperimentalTools.Actions(["open_app"], "macos").Single();
        var input = JsonSerializer.SerializeToElement(new { app = $"NoSuchJarvisApp\"; touch '{marker}'; \"" });

        var (ok, _) = await CommandRunner.RunAsync(action, input, CancellationToken.None);

        Assert.False(File.Exists(marker));
        Assert.False(ok);
    }

    [Fact]
    public async Task On_macos_a_media_command_outside_the_three_is_refused_without_running()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var marker = Path.Combine(_dir, "pwned-media");
        var action = ExperimentalTools.Actions(["media_control"], "macos").Single();

        var (ok, _) = await CommandRunner.RunAsync(action, JsonSerializer.SerializeToElement(new { command = $"next; touch '{marker}'" }), CancellationToken.None);

        Assert.False(ok);
        Assert.False(File.Exists(marker));
    }

    [Fact]
    public async Task On_macos_a_timer_with_a_shell_payload_is_refused_before_anything_is_scheduled()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var marker = Path.Combine(_dir, "pwned-timer");
        var action = ExperimentalTools.Actions(["timer"], "macos").Single();
        var input = JsonSerializer.SerializeToElement(new { minutes = $"5; touch '{marker}'", text = "x" });

        var (ok, _) = await CommandRunner.RunAsync(action, input, CancellationToken.None);

        Assert.False(ok);
        Assert.False(File.Exists(marker));
    }

    [Fact]
    public async Task On_macos_a_volume_with_a_shell_payload_is_refused()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var marker = Path.Combine(_dir, "pwned-volume");
        var action = ExperimentalTools.Actions(["volume"], "macos").Single();

        var (ok, _) = await CommandRunner.RunAsync(action, JsonSerializer.SerializeToElement(new { level = $"5; touch '{marker}'" }), CancellationToken.None);

        Assert.False(ok);
        Assert.False(File.Exists(marker));
    }

    public void Dispose() => Directory.Delete(_dir, true);
}
