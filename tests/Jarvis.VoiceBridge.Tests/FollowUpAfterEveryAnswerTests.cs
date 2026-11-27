namespace Jarvis.VoiceBridge.Tests;

/// <summary>D11 at the bridge: the window does not open after a cancelled answer or while muted.</summary>
public sealed class FollowUpAfterEveryAnswerTests
{
    [Fact]
    public async Task A_cancelled_answer_does_not_open_the_window()
    {
        await using var bed = new BridgeTestBed();
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();

        await bed.Server.SendAsync("""{"type":"done","cancelled":true}""");
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Idle, "idle after a cancelled answer");
        await Wait.SettleAsync();

        Assert.Equal(1, bed.Output.Count(BridgeStates.Listening));
    }

    [Fact]
    public async Task A_muted_microphone_does_not_open_the_window()
    {
        await using var bed = new BridgeTestBed();
        await bed.SpeakToJarvisAsync();
        await bed.FinishUtteranceAsync();
        await bed.Bridge.SetMutedAsync(true, CancellationToken.None);

        await bed.Server.SendAsync("""{"type":"done"}""");
        await Wait.UntilAsync(() => bed.Output.Last == BridgeStates.Muted, "muted after the answer");
        await Wait.SettleAsync();

        Assert.Equal(1, bed.Output.Count(BridgeStates.Listening));
    }
}
