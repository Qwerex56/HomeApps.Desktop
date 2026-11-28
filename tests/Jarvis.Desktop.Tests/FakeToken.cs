using Jarvis.VoiceBridge;

namespace Jarvis.Desktop.Tests;

internal sealed class FakeToken : IAccessToken
{
    public const string Value = "homeapps-token";

    public string Kind => "computer";

    public Task<string> AccessTokenAsync(CancellationToken cancellationToken) => Task.FromResult(Value);

    public void Forget()
    {
    }
}
