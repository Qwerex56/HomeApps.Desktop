using Jarvis.VoiceBridge;

namespace Jarvis.Desktop.Tests;

public class VoiceConfirmationDisabledTests
{
    [Theory]
    [InlineData("tak")]
    [InlineData("Tak.")]
    [InlineData("nie")]
    [InlineData(" NIE! ")]
    public void ABareYesOrNo_IsNotAnAnswer(string spoken) => Assert.True(VoiceAnswers.IsYesNo(spoken));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("tak dodaj mleko")]
    [InlineData("która godzina")]
    public void OtherSpeech_IsKept(string? spoken) => Assert.False(VoiceAnswers.IsYesNo(spoken));

    [Fact]
    public void WhenAnActionWaits_TheTrayAppIsToldToAsk_NotTheMicrophone()
    {
        var asked = 0;
        var output = new HostedOutput(_ => { }, () => asked++);

        output.Confirm();

        Assert.Equal(1, asked);
    }
}
