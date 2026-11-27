using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace Jarvis.VoiceBridge.Tests;

/// <summary>A <c>/jarvis/voice</c> without JARVIS: accepts one socket, records what the bridge sends, sends what the test says.</summary>
internal sealed class FakeVoiceServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly TaskCompletionSource<WebSocket> _socket = new();
    private int _binary;

    public FakeVoiceServer()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        Url = new Uri($"http://localhost:{port}/");
        _listener.Prefixes.Add(Url.ToString());
        _listener.Start();
        _ = AcceptAsync();
    }

    public Uri Url { get; }

    /// <summary>The text messages the bridge sent, e.g. <c>{"type":"cancel"}</c>.</summary>
    public ConcurrentQueue<string> Texts { get; } = new();

    /// <summary>How many audio frames arrived.</summary>
    public int Binary => Volatile.Read(ref _binary);

    public async Task SendAsync(string json) =>
        await (await _socket.Task).SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, _stop.Token);

    public async Task SendBinaryAsync(byte[] bytes) =>
        await (await _socket.Task).SendAsync(bytes, WebSocketMessageType.Binary, true, _stop.Token);

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Close();
        _stop.Dispose();
    }

    private async Task AcceptAsync()
    {
        try
        {
            var context = await _listener.GetContextAsync();
            var socket = (await context.AcceptWebSocketAsync(null)).WebSocket;
            _socket.TrySetResult(socket);
            var buffer = new byte[64 * 1024];
            while (socket.State == WebSocketState.Open)
            {
                var received = await socket.ReceiveAsync(buffer, _stop.Token);
                if (received.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }

                if (received.MessageType == WebSocketMessageType.Binary)
                {
                    Interlocked.Increment(ref _binary);
                }
                else
                {
                    Texts.Enqueue(Encoding.UTF8.GetString(buffer, 0, received.Count));
                }
            }
        }
        catch (Exception exception) when (exception is HttpListenerException or WebSocketException or OperationCanceledException or ObjectDisposedException)
        {
            // Test over.
        }
    }
}
