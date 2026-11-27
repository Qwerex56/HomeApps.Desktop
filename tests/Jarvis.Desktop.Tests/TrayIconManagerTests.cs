using HomeApps.Desktop;

namespace Jarvis.Desktop.Tests;

public class TrayIconManagerTests
{
    private static (TrayIconManager Manager, List<string> Shown) Create(bool reducedMotion)
    {
        var shown = new List<string>();
        return (new TrayIconManager(reducedMotion, shown.Add), shown);
    }

    [Fact]
    public void SetState_ShowsTheStateIcon()
    {
        var (manager, shown) = Create(reducedMotion: false);

        manager.SetState(TrayState.Idle);

        Assert.Equal(["idle"], shown);
    }

    [Fact]
    public void Tick_AnimatesListening()
    {
        var (manager, shown) = Create(reducedMotion: false);
        manager.SetState(TrayState.Listening);

        manager.Tick();
        manager.Tick();

        Assert.Equal(["listening-0", "listening-1", "listening-2"], shown);
    }

    [Fact]
    public void Tick_WrapsAround()
    {
        var (manager, _) = Create(reducedMotion: false);
        manager.SetState(TrayState.Speaking);

        for (var i = 0; i < 4; i++)
        {
            manager.Tick();
        }

        Assert.Equal("speaking-0", manager.IconKey);
    }

    [Fact]
    public void Tick_DoesNotAnimateIdle()
    {
        var (manager, shown) = Create(reducedMotion: false);
        manager.SetState(TrayState.Idle);

        manager.Tick();

        Assert.Single(shown);
    }

    [Theory]
    [InlineData(TrayState.Listening, "listening")]
    [InlineData(TrayState.Thinking, "thinking")]
    [InlineData(TrayState.Speaking, "speaking")]
    public void ReducedMotion_ShowsAStillIcon(TrayState state, string key)
    {
        var (manager, shown) = Create(reducedMotion: true);
        manager.SetState(state);

        manager.Tick();
        manager.Tick();

        Assert.Equal([key], shown);
        Assert.False(manager.IsAnimating);
    }

    [Theory]
    [InlineData("connecting", TrayState.Connecting)]
    [InlineData("idle", TrayState.Idle)]
    [InlineData("listening", TrayState.Listening)]
    [InlineData("thinking", TrayState.Thinking)]
    [InlineData("speaking", TrayState.Speaking)]
    [InlineData("muted", TrayState.Muted)]
    public void SetState_MapsBridgeStates(string bridge, TrayState expected)
    {
        var (manager, _) = Create(reducedMotion: true);

        manager.SetState(bridge);

        Assert.Equal(expected, manager.State);
    }

    [Fact]
    public void SetState_IgnoresUnknownBridgeState()
    {
        var (manager, shown) = Create(reducedMotion: true);

        manager.SetState("bogus");

        Assert.Empty(shown);
        Assert.Equal(TrayState.Connecting, manager.State);
    }
}
