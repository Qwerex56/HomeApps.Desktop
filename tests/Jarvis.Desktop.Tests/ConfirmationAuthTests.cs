using Jarvis.VoiceBridge;

namespace Jarvis.Desktop.Tests;

public class ConfirmationAuthTests
{
    private const string Id = "6f1c0a52-3b1e-4f43-9d0c-0a1b2c3d4e5f";

    private sealed class Answering(bool? answer) : HomeApps.Desktop.INotifier
    {
        public int Asked { get; private set; }

        public Task<bool?> AskAsync(ActionView action, CancellationToken cancellationToken)
        {
            Asked++;
            return Task.FromResult(answer);
        }
    }

    private static StubServer Server()
    {
        var server = new StubServer();
        server.Replies["GET /jarvis/api/v1/confirmations?pending=true"] =
            $$"""[{"id":"{{Id}}","tool":"todo.delete","description":"Usun liste","status":"NeedsConfirmation","confirmBy":null}]""";
        server.Replies[$"POST /jarvis/api/v1/confirmations/{Id}"] =
            $$"""{"id":"{{Id}}","tool":"todo.delete","description":"ok","status":"Done","confirmBy":null}""";
        return server;
    }

    [Fact]
    public async Task Yes_SendsTheHomeAppsTokenAndGets200()
    {
        var server = Server();
        var notifier = new Answering(true);

        var decided = await new HomeApps.Desktop.ConfirmationNotifier(server.Api(), notifier).CheckAsync(default);

        Assert.Equal(1, decided);
        var post = Assert.Single(server.Seen, s => s.Request.StartsWith("POST", StringComparison.Ordinal));
        Assert.Equal(FakeToken.Value, post.Bearer);
        Assert.Contains("\"approve\":true", post.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task No_SendsApproveFalse_AndAnActionIsAskedOnlyOnce()
    {
        var server = Server();
        var notifier = new Answering(false);
        var checker = new HomeApps.Desktop.ConfirmationNotifier(server.Api(), notifier);

        await checker.CheckAsync(default);
        await checker.CheckAsync(default);

        Assert.Equal(1, notifier.Asked);
        Assert.Contains("\"approve\":false", server.Seen.Single(s => s.Request.StartsWith("POST", StringComparison.Ordinal)).Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Dismissed_SendsNothing()
    {
        var server = Server();

        var decided = await new HomeApps.Desktop.ConfirmationNotifier(server.Api(), new Answering(null)).CheckAsync(default);

        Assert.Equal(0, decided);
        Assert.DoesNotContain(server.Seen, s => s.Request.StartsWith("POST", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnotherAudience_IsRefusedWith403()
    {
        var server = Server();
        using var http = new HttpClient(server);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://home.test/jarvis/api/v1/confirmations?pending=true");
        request.Headers.Authorization = new("Bearer", "mcp-audience-token");

        using var response = await http.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public void TheDesktopSignsInAsTheFirstPartyClientForHomeApps()
    {
        Assert.Equal("homeapps-desktop", OAuthLogin.FirstPartyClient);
        Assert.Equal("homeapps", OAuthLogin.FirstPartyResource);
    }
}
