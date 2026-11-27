namespace Jarvis.VoiceBridge.Tests;

internal static class Wait
{
    public static async Task UntilAsync(Func<bool> condition, string what)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, $"Timed out waiting for: {what}");
            await Task.Delay(20);
        }
    }

    /// <summary>Gives the bridge time to (wrongly) do something, then the test checks it did not.</summary>
    public static Task SettleAsync() => Task.Delay(400);
}
