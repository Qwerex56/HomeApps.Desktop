using System.Diagnostics;

namespace Jarvis.VoiceBridge;

/// <summary>The bridge inside the tray app: states go to the app, the sign-in page opens in the browser, the rest is logged to stderr.</summary>
internal sealed class HostedOutput(Action<string> onState) : IBridgeOutput
{
    public void State(string state) => onState(state);

    public void Level(double level)
    {
    }

    public void Heard(string? text) => Console.Error.WriteLine($"Ty: {text}");

    public void Reply(string text, IReadOnlyList<ReplyAction> actions) => Console.Error.WriteLine($"JARVIS: {text}");

    public void Confirm()
    {
    }

    public void Error(string? code, string? detail) => Console.Error.WriteLine($"Błąd serwera: {code} {detail}".TrimEnd());

    public void Login(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true })?.Dispose();
        }
    }
}
