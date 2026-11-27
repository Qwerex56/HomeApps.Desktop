using System.Text.RegularExpressions;

namespace Jarvis.VoiceBridge;

/// <summary>The words that interrupt JARVIS while he speaks (requirement 2.1): "stop" and "cisza".</summary>
internal static partial class StopWords
{
    public static bool IsStop(string? text) =>
        !string.IsNullOrWhiteSpace(text)
        && Word().Matches(text).Any(m => m.Value.Equals("stop", StringComparison.OrdinalIgnoreCase)
            || m.Value.Equals("cisza", StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex Word();
}
