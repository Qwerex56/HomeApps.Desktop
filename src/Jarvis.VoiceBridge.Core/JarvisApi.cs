using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jarvis.VoiceBridge;

/// <summary>JARVIS's REST API for the tray app: the person's conversations and messages (2.5) and the confirmations (2.3, 2.4).</summary>
internal sealed class JarvisApi(HttpClient http, Uri server, IAccessToken login)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<IReadOnlyList<ConversationView>> ConversationsAsync(string? query, CancellationToken cancellationToken)
    {
        var path = "jarvis/api/v1/conversations" + (string.IsNullOrWhiteSpace(query) ? string.Empty : "?q=" + Uri.EscapeDataString(query.Trim()));
        return await GetAsync<List<ConversationView>>(path, cancellationToken);
    }

    public async Task<IReadOnlyList<MessageView>> MessagesAsync(Guid conversationId, CancellationToken cancellationToken) =>
        await GetAsync<List<MessageView>>($"jarvis/api/v1/conversations/{conversationId}/messages", cancellationToken);

    public async Task<IReadOnlyList<ActionView>> PendingAsync(CancellationToken cancellationToken) =>
        await GetAsync<List<ActionView>>("jarvis/api/v1/confirmations?pending=true", cancellationToken);

    /// <summary>Yes or no to an action that waits; the person's own token (audience <c>homeapps</c>).</summary>
    public async Task<ActionView> DecideAsync(Guid actionId, bool approve, CancellationToken cancellationToken)
    {
        using var request = await RequestAsync(HttpMethod.Post, $"jarvis/api/v1/confirmations/{actionId}", cancellationToken);
        request.Content = JsonContent.Create(new { approve }, options: Json);
        return await SendAsync<ActionView>(request, cancellationToken);
    }

    public async Task<ConversationView> StartConversationAsync(CancellationToken cancellationToken)
    {
        using var request = await RequestAsync(HttpMethod.Post, "jarvis/api/v1/conversations", cancellationToken);
        request.Content = JsonContent.Create(new { title = (string?)null }, options: Json);
        return await SendAsync<ConversationView>(request, cancellationToken);
    }

    /// <summary>Sends a typed message and waits for the kept answer (the stream's <c>message</c> event); <c>error</c> throws.</summary>
    public async Task<MessageView> SendAsync(Guid conversationId, string text, CancellationToken cancellationToken)
    {
        using var request = await RequestAsync(HttpMethod.Post, $"jarvis/api/v1/conversations/{conversationId}/messages", cancellationToken);
        request.Content = JsonContent.Create(new { text }, options: Json);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(cancellationToken));
        string? name = null;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                name = line[6..].Trim();
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal) && name == "message")
            {
                return JsonSerializer.Deserialize<MessageView>(line[5..], Json) ?? throw new InvalidOperationException("Empty answer.");
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal) && name == "error")
            {
                throw new InvalidOperationException(line[5..].Trim());
            }
        }

        throw new InvalidOperationException("The answer ended without a message.");
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = await RequestAsync(HttpMethod.Get, path, cancellationToken);
        return await SendAsync<T>(request, cancellationToken);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken) ?? throw new InvalidOperationException("Empty response.");
    }

    private async Task<HttpRequestMessage> RequestAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, new Uri(server, path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await login.AccessTokenAsync(cancellationToken));
        return request;
    }
}
