using System.Net;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>jarvis-satellites.md S-1e: <c>--pair</c> makes the bridge its household's satellite.</summary>
public sealed class SatelliteLoginTests : IDisposable
{
    private static readonly Guid Device = Guid.NewGuid();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "bridge-" + Guid.NewGuid());
    private readonly FakeServer _server = new();
    private readonly HttpClient _http;
    private readonly TokenStore _store;
    private readonly SatelliteLogin _login;

    public SatelliteLoginTests()
    {
        var options = BridgeOptions.Parse(["--server", "https://home.test"]);
        _http = new HttpClient(_server);
        _store = new TokenStore(options.Server, _folder);
        _login = new SatelliteLogin(_http, options, _store);
        _server.Answers["/devices/pair"] = (HttpStatusCode.OK,
            $$"""{"deviceId":"{{Device}}","householdId":"{{Guid.NewGuid()}}","name":"salon","secret":"s3cret"}""");
        _server.Answers["/auth/devices/token"] = (HttpStatusCode.OK,
            $$"""{"accessToken":"device-token","expiresAt":"{{DateTimeOffset.UtcNow.AddMinutes(15):O}}"}""");
    }

    public void Dispose()
    {
        _http.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public async Task The_code_is_traded_once_for_a_secret_that_then_buys_short_tokens()
    {
        var paired = await _login.PairAsync("abcd-efgh", CancellationToken.None);
        var first = await _login.AccessTokenAsync(CancellationToken.None);
        var second = await _login.AccessTokenAsync(CancellationToken.None);

        Assert.Equal("salon", paired.Name);
        Assert.True(SatelliteLogin.IsPaired(_store));
        Assert.Equal(("device-token", "device-token"), (first, second));
        Assert.Single(_server.Asked, a => a.Path == "/auth/devices/token");
        Assert.Contains("s3cret", _server.Asked.Single(a => a.Path == "/auth/devices/token").Body, StringComparison.Ordinal);
        Assert.Equal("satellite", _login.Kind);
    }

    [Fact]
    public async Task A_used_or_expired_code_pairs_nothing()
    {
        _server.Answers["/devices/pair"] = (HttpStatusCode.NotFound, """{"code":"devices.pairing_expired"}""");

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => _login.PairAsync("abcd-efgh", CancellationToken.None));

        Assert.Contains("nie działa", refused.Message, StringComparison.Ordinal);
        Assert.False(SatelliteLogin.IsPaired(_store));
    }

    [Fact]
    public async Task A_server_that_cannot_pair_now_says_so_and_keeps_the_code_usable()
    {
        _server.Answers["/devices/pair"] = (HttpStatusCode.Conflict,
            """{"title":"Conflict","status":409,"code":"devices.pairing_unavailable","detail":"Pairing is not available right now."}""");

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => _login.PairAsync("abcd-efgh", CancellationToken.None));

        Assert.Contains("Kod nadal jest ważny", refused.Message, StringComparison.Ordinal);
        Assert.Contains("HTTP 409 devices.pairing_unavailable", refused.Message, StringComparison.Ordinal);
        Assert.False(SatelliteLogin.IsPaired(_store));
    }

    [Fact]
    public async Task Any_other_refusal_is_shown_with_its_status_even_without_a_problem_body()
    {
        _server.Answers["/devices/pair"] = (HttpStatusCode.BadGateway, "<html>Bad gateway</html>");

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => _login.PairAsync("abcd-efgh", CancellationToken.None));

        Assert.Equal("Parowanie się nie udało (HTTP 502).", refused.Message);
    }

    [Fact]
    public async Task A_satellite_removed_in_the_household_forgets_its_pairing()
    {
        await _login.PairAsync("abcd-efgh", CancellationToken.None);
        _server.Answers["/auth/devices/token"] = (HttpStatusCode.Unauthorized, "{}");

        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => _login.AccessTokenAsync(CancellationToken.None));

        Assert.Contains("--pair", refused.Message, StringComparison.Ordinal);
        Assert.False(SatelliteLogin.IsPaired(_store));
    }

    [Fact]
    public void Pairing_is_read_from_the_command_line()
    {
        var options = BridgeOptions.Parse(["--pair", "ABCD-EFGH", "--unpair"]);

        Assert.Equal("ABCD-EFGH", options.Pair);
        Assert.True(options.Unpair);
    }
}
