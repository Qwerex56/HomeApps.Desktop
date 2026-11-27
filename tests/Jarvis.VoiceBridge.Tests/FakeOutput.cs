using System.Collections.Concurrent;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>Collects the states the bridge reports.</summary>
internal sealed class FakeOutput : IBridgeOutput
{
    public ConcurrentQueue<string> States { get; } = new();

    public string? Last => States.LastOrDefault();

    public int Count(string state) => States.Count(s => s == state);

    public void State(string state) => States.Enqueue(state);

    public void Level(double level)
    {
    }

    public void Heard(string? text)
    {
    }

    public void Reply(string text, IReadOnlyList<ReplyAction> actions)
    {
    }

    public void Confirm()
    {
    }

    public void Error(string? code, string? detail)
    {
    }

    public void Login(string url)
    {
    }
}
