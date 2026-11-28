using Jarvis.VoiceBridge;

namespace HomeApps.Desktop;

/// <summary>What the history window shows (2.5): the account's conversations (search with <c>?q=</c>), the open one's messages under the day and channel filters. View-only: nothing is deleted here.</summary>
internal sealed class HistoryModel(JarvisApi api)
{
    private IReadOnlyList<MessageView> _messages = [];

    public IReadOnlyList<ConversationView> Conversations { get; private set; } = [];

    public ConversationView? Selected { get; private set; }

    public DateOnly? Day { get; set; }

    public string? Channel { get; set; }

    public IReadOnlyList<MessageView> Visible => HistoryFilter.Apply(_messages, Day, Channel);

    /// <summary>Loads the conversations (those with <paramref name="query"/> in their messages when given) and opens the latest.</summary>
    public async Task SearchAsync(string? query, CancellationToken cancellationToken)
    {
        Conversations = (await api.ConversationsAsync(query, cancellationToken)).OrderByDescending(c => c.LastActivityAt).ToList();
        await SelectAsync((Conversations.Count > 0 ? Conversations[0] : null), cancellationToken);
    }

    public async Task SelectAsync(ConversationView? conversation, CancellationToken cancellationToken)
    {
        Selected = conversation;
        _messages = conversation is null ? [] : await api.MessagesAsync(conversation.Id, cancellationToken);
    }

    /// <summary>The text field at the bottom: sends to the open conversation (a new one when none is open) and shows the answer.</summary>
    public async Task SendAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var conversation = Selected ?? await api.StartConversationAsync(cancellationToken);
        await api.SendAsync(conversation.Id, text.Trim(), cancellationToken);
        await SelectAsync(conversation, cancellationToken);
        Conversations = Conversations.Any(c => c.Id == conversation.Id) ? Conversations : [conversation, .. Conversations];
    }

    /// <summary>Yes or no to a waiting action in the open conversation; the messages are read again so the card shows the result.</summary>
    public async Task DecideAsync(Guid actionId, bool approve, CancellationToken cancellationToken)
    {
        await api.DecideAsync(actionId, approve, cancellationToken);
        await SelectAsync(Selected, cancellationToken);
    }
}
