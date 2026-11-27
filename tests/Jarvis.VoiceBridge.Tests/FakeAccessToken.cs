namespace Jarvis.VoiceBridge.Tests;

internal sealed class FakeAccessToken : IAccessToken
{
    public string Kind => "computer";

    public Task<string> AccessTokenAsync(CancellationToken cancellationToken) => Task.FromResult("token");

    public void Forget()
    {
    }
}
