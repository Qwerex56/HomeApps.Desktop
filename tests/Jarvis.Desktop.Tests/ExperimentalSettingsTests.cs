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

    public void Dispose() => Directory.Delete(_dir, true);
}
