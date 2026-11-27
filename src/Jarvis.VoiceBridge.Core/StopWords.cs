using System.Text.RegularExpressions;

namespace Jarvis.VoiceBridge;

/// <summary>The words that interrupt JARVIS while he speaks (requirement 2.1): "stop" and "cisza".</summary>
internal static partial class StopWords
{
    /// <summary>Only a short utterance (at most <see cref="LongestWords"/> words) counts, so a sentence that merely contains "stop" does not.</summary>
    public const int LongestWords = 3;

    public static bool IsStop(string? text) =>
        !string.IsNullOrWhiteSpace(text)
        && Word().Matches(text) is { Count: > 0 and <= LongestWords } words
        && words.Any(m => m.Value.Equals("stop", StringComparison.OrdinalIgnoreCase)
            || m.Value.Equals("cisza", StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex Word();
}
