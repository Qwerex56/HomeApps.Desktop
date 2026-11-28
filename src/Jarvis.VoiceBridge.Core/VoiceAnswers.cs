using System.Text.RegularExpressions;

namespace Jarvis.VoiceBridge;

/// <summary>D8: a confirmation is answered on the screen only; a bare spoken "tak" or "nie" is not an answer and is dropped.</summary>
internal static partial class VoiceAnswers
{
    public static bool IsYesNo(string? text) =>
        !string.IsNullOrWhiteSpace(text)
        && Word().Matches(text) is { Count: 1 } words
        && words[0].Value.ToLowerInvariant() is "tak" or "nie";

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex Word();
}
