using HomeApps.Desktop;

namespace Jarvis.Desktop.Tests;

public class MuteSwitchTests
{
    [Fact]
    public void Toggle_FlipsAndReports()
    {
        var seen = new List<bool>();
        var mute = new MuteSwitch(seen.Add);

        mute.Toggle();
        mute.Toggle();

        Assert.Equal([true, false], seen);
        Assert.False(mute.IsMuted);
    }
}
