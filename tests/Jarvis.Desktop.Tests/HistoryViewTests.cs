using HomeApps.Desktop;
using Jarvis.VoiceBridge;

namespace Jarvis.Desktop.Tests;

public class HistoryViewTests
{
    private const string Conversation = "11111111-1111-1111-1111-111111111111";
    private const string Action = "22222222-2222-2222-2222-222222222222";

    private static MessageView Message(string channel, DateTimeOffset at) =>
        new(Guid.NewGuid(), MessageRole.Person, "x", at, [], channel);

    private static StubServer Server()
    {
        var server = new StubServer();
        server.Replies["GET /jarvis/api/v1/conversations"] = $$"""[{"id":"{{Conversation}}","title":"Dom","lastActivityAt":"2026-11-27T10:00:00+00:00"}]""";
        server.Replies["GET /jarvis/api/v1/conversations?q=mleko"] = server.Replies["GET /jarvis/api/v1/conversations"];
        server.Replies[$"GET /jarvis/api/conversations/{Conversation}/messages"] = $$"""
            [{"id":"{{Guid.NewGuid()}}","role":"Person","text":"usun liste","sentAt":"2026-11-27T10:00:00+00:00","actions":[],"channel":"voice"},
             {"id":"{{Guid.NewGuid()}}","role":"Assistant","text":"Czekam","sentAt":"2026-11-27T10:00:05+00:00","channel":"voice",
              "actions":[{"id":"{{Action}}","tool":"todo.delete","description":"Usun liste","status":"NeedsConfirmation","confirmBy":null}]},
             {"id":"{{Guid.NewGuid()}}","role":"Person","text":"hej","sentAt":"2026-11-28T10:00:00+00:00","actions":[],"channel":"app"}]
            """;
        server.Replies[$"POST /jarvis/api/conversations/{Conversation}/messages"] =
            $$"""
            event: text
            data: "Ok"

            event: message
            data: {"id":"{{Guid.NewGuid()}}","role":"Assistant","text":"Ok","sentAt":"2026-11-28T11:00:00+00:00","actions":[],"channel":"desktop"}

            """;
        return server;
    }

    [Fact]
    public void Filter_ByChannelAndDay()
    {
        var day1 = new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 11, 27, 12, 0, 0)));
        var messages = new[] { Message("voice", day1), Message("app", day1), Message("voice", day1.AddDays(1)) };

        Assert.Equal(2, HistoryFilter.Apply(messages, null, "voice").Count);
        Assert.Equal(2, HistoryFilter.Apply(messages, new DateOnly(2026, 11, 27), null).Count);
        Assert.Single(HistoryFilter.Apply(messages, new DateOnly(2026, 11, 27), "voice"));
        Assert.Equal(3, HistoryFilter.Apply(messages, null, "").Count);
    }

    [Fact]
    public async Task Search_SendsQ_AndShowsTheToolCallWithItsConfirmation()
    {
        var server = Server();
        var model = new HistoryModel(server.Api());

        await model.SearchAsync("mleko", default);

        Assert.Contains(server.Seen, s => s.Request == "GET /jarvis/api/v1/conversations?q=mleko");
        Assert.Equal(3, model.Visible.Count);
        var action = Assert.Single(model.Visible.SelectMany(m => m.Actions));
        Assert.Equal(ToolCallStatus.NeedsConfirmation, action.Status);
    }

    [Fact]
    public async Task ChannelFilter_NarrowsTheOpenConversation()
    {
        var model = new HistoryModel(Server().Api());
        await model.SearchAsync(null, default);

        model.Channel = "app";

        Assert.Equal("hej", Assert.Single(model.Visible).Text);
    }

    [Fact]
    public async Task BottomInput_SendsToTheOpenConversation_AndBlankTextSendsNothing()
    {
        var server = Server();
        var model = new HistoryModel(server.Api());
        await model.SearchAsync(null, default);

        await model.SendAsync("  ", default);
        Assert.DoesNotContain(server.Seen, s => s.Request.StartsWith("POST", StringComparison.Ordinal));

        await model.SendAsync("kup mleko", default);
        var post = Assert.Single(server.Seen, s => s.Request.StartsWith("POST", StringComparison.Ordinal));
        Assert.Contains("kup mleko", post.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task History_NeverDeletesAnything()
    {
        var server = Server();
        server.Replies[$"POST /jarvis/api/v1/confirmations/{Action}"] = "{}";
        var model = new HistoryModel(server.Api());
        await model.SearchAsync("mleko", default);
        await model.SelectAsync(model.Conversations[0], default);
        await model.SendAsync("kup mleko", default);
        await model.DecideAsync(Guid.Parse(Action), true, default);

        Assert.NotEmpty(server.Seen);
        Assert.DoesNotContain(server.Seen, s => s.Request.StartsWith("DELETE", StringComparison.Ordinal));
    }
}
