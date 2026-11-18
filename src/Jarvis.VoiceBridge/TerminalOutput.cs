namespace Jarvis.VoiceBridge;

/// <summary>The bridge in a terminal: the conversation as text, states and loudness are not shown.</summary>
internal sealed class TerminalOutput : IBridgeOutput
{
    public void State(string state)
    {
    }

    public void Level(double level)
    {
    }

    public void Heard(string? text) =>
        Console.WriteLine(string.IsNullOrWhiteSpace(text) ? "Ty: (nic nie usłyszałem)" : $"Ty: {text}");

    public void Reply(string text, IReadOnlyList<ReplyAction> actions)
    {
        Console.WriteLine($"JARVIS: {text}");
        foreach (var action in actions)
        {
            Console.WriteLine($"  [{action.Status}] {action.Description}");
        }
    }

    public void Confirm() => Console.WriteLine("(tak / nie?)");

    public void Error(string? code, string? detail) => Console.WriteLine($"Błąd serwera: {code} {detail}".TrimEnd());

    public void Login(string url)
    {
        Console.WriteLine("Otwieram przeglądarkę: zaloguj się w HomeApps i kliknij „Zezwól”. Jeśli się nie otworzyła:");
        Console.WriteLine(url);
    }
}
