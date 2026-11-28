using System.Net;
using System.Text;
using Jarvis.VoiceBridge;

namespace Jarvis.Desktop.Tests;

/// <summary>A fake JARVIS server: answers by "METHOD path" and records every request with its bearer token.</summary>
internal sealed class StubServer : HttpMessageHandler
{
    public Dictionary<string, string> Replies { get; } = [];

    public List<(string Request, string? Bearer, string Body)> Seen { get; } = [];

    public JarvisApi Api() => new(new HttpClient(this), new Uri("https://home.test/"), new FakeToken());

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var key = $"{request.Method} {request.RequestUri!.PathAndQuery}";
        Seen.Add((key, request.Headers.Authorization?.Parameter, request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)));
        if (request.Headers.Authorization?.Parameter != FakeToken.Value || !Replies.TryGetValue(key, out var body))
        {
            return new HttpResponseMessage(request.Headers.Authorization?.Parameter != FakeToken.Value ? HttpStatusCode.Forbidden : HttpStatusCode.NotFound);
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, key.Contains("/messages", StringComparison.Ordinal) && key.StartsWith("POST", StringComparison.Ordinal) ? "text/event-stream" : "application/json") };
    }
}
