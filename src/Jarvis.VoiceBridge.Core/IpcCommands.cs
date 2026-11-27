using System.Text.Json;

namespace Jarvis.VoiceBridge;

/// <summary>Reads one stdin line of <c>--ipc</c>: <c>{"type":"talk"}</c>; anything else is <c>null</c>.</summary>
internal static class IpcCommands
{
    public static IpcCommand? Parse(string line)
    {
        try
        {
            using var json = JsonDocument.Parse(line);
            return json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("type", out var type)
                && type.ValueKind == JsonValueKind.String
                    ? type.GetString() switch
                    {
                        "talk" => IpcCommand.Talk,
                        "stop" => IpcCommand.Stop,
                        "mute" => IpcCommand.Mute,
                        "unmute" => IpcCommand.Unmute,
                        "quit" => IpcCommand.Quit,
                        _ => null,
                    }
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
