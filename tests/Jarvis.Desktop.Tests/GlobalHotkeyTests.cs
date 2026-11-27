using HomeApps.Desktop;
using SharpHook.Data;

namespace Jarvis.Desktop.Tests;

public class GlobalHotkeyTests
{
    [Theory]
    [InlineData(EventMask.LeftCtrl | EventMask.LeftAlt)]
    [InlineData(EventMask.RightCtrl | EventMask.RightAlt)]
    [InlineData(EventMask.LeftCtrl | EventMask.RightAlt)]
    [InlineData(EventMask.Ctrl | EventMask.Alt)]
    public void IsMuteShortcut_AcceptsEitherSide(EventMask mask) =>
        Assert.True(GlobalHotkey.IsMuteShortcut(KeyCode.VcM, mask));

    [Theory]
    [InlineData(EventMask.LeftCtrl)]
    [InlineData(EventMask.LeftAlt)]
    [InlineData(EventMask.None)]
    public void IsMuteShortcut_NeedsBothModifiers(EventMask mask) =>
        Assert.False(GlobalHotkey.IsMuteShortcut(KeyCode.VcM, mask));

    [Fact]
    public void IsMuteShortcut_NeedsTheMKey() =>
        Assert.False(GlobalHotkey.IsMuteShortcut(KeyCode.VcN, EventMask.LeftCtrl | EventMask.LeftAlt));
}
