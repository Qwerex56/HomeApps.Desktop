namespace Jarvis.VoiceBridge.Tests;

/// <summary>jarvis-satellites.md S-2b: what the hologram app may ask of the bridge.</summary>
public sealed class IpcCommandsTests
{
    [Theory]
    [InlineData("""{"type":"talk"}""", "Talk")]
    [InlineData("""{"type":"stop"}""", "Stop")]
    [InlineData("""{"type":"mute"}""", "Mute")]
    [InlineData("""{"type":"unmute"}""", "Unmute")]
    [InlineData("""{"type":"quit"}""", "Quit")]
    public void Known_commands_are_read(string line, string command) =>
        Assert.Equal(Enum.Parse<IpcCommand>(command), IpcCommands.Parse(line));

    [Theory]
    [InlineData("talk")]
    [InlineData("""{"type":"dance"}""")]
    [InlineData("""{"type":1}""")]
    [InlineData("[]")]
    public void Anything_else_is_not_a_command(string line) => Assert.Null(IpcCommands.Parse(line));
}
