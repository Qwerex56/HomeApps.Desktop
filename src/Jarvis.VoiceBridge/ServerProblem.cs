using System.Net.Http.Json;
using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>
/// A refusal from HomeApps: its ProblemDetails carry <c>code</c> (e.g. <c>devices.pairing_expired</c>) and
/// <c>detail</c>. Anything else (a proxy's HTML page, an empty body) leaves both empty and the status speaks alone.
/// </summary>
internal sealed record ServerProblem(int Status, string? Code, string? Detail)
{
    public static async Task<ServerProblem> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        try
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return new ServerProblem(status, Text(body, "code"), Text(body, "detail") ?? Text(body, "title"));
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return new ServerProblem(status, null, null);
        }
    }

    public override string ToString() =>
        $"HTTP {Status}{(Code is null ? string.Empty : $" {Code}")}{(Detail is null ? string.Empty : $": {Detail}")}";

    private static string? Text(JsonElement body, string name) =>
        body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
