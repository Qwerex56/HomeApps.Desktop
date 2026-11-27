using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>
/// <c>--ipc</c> (jarvis-satellites.md S-2b, H3): one JSON object per line on stdout, e.g. <c>{"type":"state","state":"listening"}</c>.
/// Everything else the bridge prints goes to stderr, so stdout stays machine-readable. A repeated state is not sent again.
/// </summary>
internal sealed class IpcOutput(TextWriter lines) : IBridgeOutput
{
    private readonly Lock _write = new();
    private string? _state;

    public void State(string state)
    {
        lock (_write)
        {
            if (state == _state)
            {
                return;
            }

            _state = state;
        }

        Send(new { type = "state", state });
    }

    public void Level(double level) =>
        Send(new { type = "level", level = Math.Round(Math.Clamp(level, 0, 1), 2) });

    public void Heard(string? text) => Send(new { type = "heard", text = text ?? string.Empty });

    public void Reply(string text, IReadOnlyList<ReplyAction> actions) =>
        Send(new { type = "reply", text, actions = actions.Select(a => new { status = a.Status, description = a.Description }) });

    public void Confirm() => Send(new { type = "confirm" });

    public void Error(string? code, string? detail) => Send(new { type = "error", code, detail });

    public void Login(string url) => Send(new { type = "login", url });

    private void Send(object message)
    {
        var line = JsonSerializer.Serialize(message);
        lock (_write)
        {
            lines.WriteLine(line);
            lines.Flush();
        }
    }
}
