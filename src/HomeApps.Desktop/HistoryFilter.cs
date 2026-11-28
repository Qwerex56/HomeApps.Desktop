using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>Requirement 2.5: the history narrowed by the day (the person's local day) and the channel (<c>app</c>, <c>voice</c>, <c>desktop</c>).</summary>
internal static class HistoryFilter
{
    public static IReadOnlyList<MessageView> Apply(IEnumerable<MessageView> messages, DateOnly? day, string? channel) =>
        messages
            .Where(m => day is null || DateOnly.FromDateTime(m.SentAt.ToLocalTime().DateTime) == day)
            .Where(m => string.IsNullOrEmpty(channel) || string.Equals(m.Channel, channel, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.SentAt)
            .ToList();
}
