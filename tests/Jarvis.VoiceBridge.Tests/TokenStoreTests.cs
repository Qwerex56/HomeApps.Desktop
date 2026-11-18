using Jarvis.VoiceBridge;

namespace Jarvis.VoiceBridge.Tests;

public sealed class TokenStoreTests
{
    [Fact]
    public void Forgetting_before_anything_was_kept_does_nothing()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"tokens-{Guid.NewGuid():N}");
        var store = new TokenStore(new Uri("https://home.test/"), folder);

        store.Delete("refresh_token");

        Assert.Null(store.Read("refresh_token"));
    }
}
