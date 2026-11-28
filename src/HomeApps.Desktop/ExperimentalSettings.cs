using System.Text.Json;
using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>"Funkcje eksperymentalne" (2.10): which computer tools are on, kept in <c>~/.homeapps/desktop/experimental.json</c>. All off by default; a change is applied only after the person confirms it on the screen (the D3 popup).</summary>
internal sealed class ExperimentalSettings(string path, INotifier popup)
{
    private readonly HashSet<string> _enabled = Load(path);

    public IReadOnlyCollection<string> Enabled => _enabled;

    public bool IsOn(string id) => _enabled.Contains(id);

    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".homeapps", "desktop", "experimental.json");

    /// <summary>Asks to switch <paramref name="id"/> to <paramref name="on"/>; returns whether it changed (Tak).</summary>
    public async Task<bool> ChangeAsync(string id, bool on, CancellationToken cancellationToken)
    {
        if (!VoiceBridgeHost.ExperimentalToolIds.Contains(id) || IsOn(id) == on)
        {
            return false;
        }

        var text = $"{(on ? "Włączyć" : "Wyłączyć")} funkcję eksperymentalną „{VoiceBridgeHost.ExperimentalToolLabel(id)}”?";
        if (await popup.AskAsync(new ActionView(Guid.NewGuid(), id, text, ToolCallStatus.NeedsConfirmation, null), cancellationToken) is not true)
        {
            return false;
        }

        if (on)
        {
            _enabled.Add(id);
        }
        else
        {
            _enabled.Remove(id);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(_enabled.Order()));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Not persisted; the choice stays in memory until the app restarts.
        }

        return true;
    }

    private static HashSet<string> Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return [];
            }

            var ids = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path));
            return ids is null ? [] : ids.Where(VoiceBridgeHost.ExperimentalToolIds.Contains).ToHashSet();
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
