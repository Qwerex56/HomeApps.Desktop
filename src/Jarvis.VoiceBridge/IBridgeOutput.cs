namespace Jarvis.VoiceBridge;

/// <summary>
/// What the bridge tells whoever runs it (jarvis-satellites.md S-2b): a person at the terminal (<see cref="TerminalOutput"/>)
/// or the hologram app reading JSON lines (<see cref="IpcOutput"/>).
/// </summary>
internal interface IBridgeOutput
{
    /// <summary>One of <see cref="BridgeStates"/>.</summary>
    void State(string state);

    /// <summary>How loud JARVIS's voice is right now, 0 to 1, about 30 times a second while he speaks.</summary>
    void Level(double level);

    void Heard(string? text);

    void Reply(string text, IReadOnlyList<ReplyAction> actions);

    /// <summary>JARVIS waits for "tak" or "nie"; the next utterance needs no wake word.</summary>
    void Confirm();

    void Error(string? code, string? detail);

    /// <summary>The person has to sign in: this address is opened in the browser.</summary>
    void Login(string url);
}
