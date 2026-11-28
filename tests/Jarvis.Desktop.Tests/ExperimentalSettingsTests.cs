using Jarvis.VoiceBridge;

namespace HomeApps.Desktop.Tests;

public sealed class ExperimentalSettingsTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    private sealed class Answer(bool? yes) : INotifier
    {
        public string? Asked;

        public Task<bool?> AskAsync(ActionView action, CancellationToken cancellationToken)
        {
            Asked = action.Description;
            return Task.FromResult(yes);
        }
    }

    [Fact]
    public void Everything_is_off_by_default()
    {
        var settings = new ExperimentalSettings(Path.Combine(_dir, "e.json"), new Answer(true));

        Assert.Empty(settings.Enabled);
    }

    [Fact]
    public async Task A_confirmed_change_is_kept_for_the_next_start()
    {
        var path = Path.Combine(_dir, "e.json");
        var popup = new Answer(true);
        var settings = new ExperimentalSettings(path, popup);

        Assert.True(await settings.ChangeAsync("volume", true, CancellationToken.None));

        Assert.Contains("Włączyć", popup.Asked);
        Assert.True(new ExperimentalSettings(path, popup).IsOn("volume"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public async Task A_change_that_is_declined_or_dismissed_does_nothing(bool? answer)
    {
        var settings = new ExperimentalSettings(Path.Combine(_dir, "e.json"), new Answer(answer));

        Assert.False(await settings.ChangeAsync("timer", true, CancellationToken.None));
        Assert.False(settings.IsOn("timer"));
    }

    [Fact]
    public async Task An_id_outside_the_six_is_refused_without_asking()
    {
        var popup = new Answer(true);
        var settings = new ExperimentalSettings(Path.Combine(_dir, "e.json"), popup);

        Assert.False(await settings.ChangeAsync("clipboard", true, CancellationToken.None));
        Assert.Null(popup.Asked);
        Assert.Empty(settings.Enabled);
    }

    [Fact]
    public async Task Turning_off_a_confirmed_tool_removes_it_for_the_next_start()
    {
        var path = Path.Combine(_dir, "e.json");
        var settings = new ExperimentalSettings(path, new Answer(true));
        await settings.ChangeAsync("timer", true, CancellationToken.None);

        Assert.True(await settings.ChangeAsync("timer", false, CancellationToken.None));

        Assert.False(new ExperimentalSettings(path, new Answer(true)).IsOn("timer"));
    }

    [Fact]
    public void Unknown_ids_in_the_file_are_ignored()
    {
        var path = Path.Combine(_dir, "e.json");
        File.WriteAllText(path, """["volume","clipboard","screenshot"]""");

        var settings = new ExperimentalSettings(path, new Answer(true));

        Assert.Equal(["volume"], settings.Enabled);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public void A_damaged_file_leaves_everything_off(string content)
    {
        var path = Path.Combine(_dir, "e.json");
        File.WriteAllText(path, content);

        Assert.Empty(new ExperimentalSettings(path, new Answer(true)).Enabled);
    }

    public void Dispose() => Directory.Delete(_dir, true);
}
