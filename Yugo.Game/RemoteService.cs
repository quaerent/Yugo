using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Yugo.Game.Screens;

namespace Yugo.Game;

public sealed class RemoteService : IDisposable
{
    private readonly Engine _engine;
    private readonly HttpListener _listener;
    private bool _running;
    private WebSocket? _activeSocket;
    private readonly object _lock = new();

    public RemoteService(Engine engine, int port = 9090)
    {
        _engine = engine;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    }

    public void Start()
    {
        _running = true;
        _listener.Start();
        Task.Run(ListenLoop);
        Console.WriteLine($"[Remote] WebSocket Server started at ws://127.0.0.1:9090/");
    }

    public async void SendMessage(string json)
    {
        WebSocket? socket;
        lock (_lock)
        {
            socket = _activeSocket;
        }
        if (socket == null || socket.State != WebSocketState.Open)
            return;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Remote] Send error: {ex.Message}");
        }
    }

    private async Task ListenLoop()
    {
        while (_running)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                    _ = HandleWebSocket(context);
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (Exception)
            {
                if (!_running)
                    break;
            }
        }
    }

    private async Task HandleWebSocket(HttpListenerContext context)
    {
        try
        {
            var wsContext = await context.AcceptWebSocketAsync(null);
            lock (_lock)
            {
                if (_activeSocket != null && _activeSocket.State == WebSocketState.Open)
                {
                    _ = RefuseConnection(wsContext.WebSocket);
                    return;
                }
                _activeSocket = wsContext.WebSocket;
            }
            Console.WriteLine("[Remote] Web client linked.");

            var buffer = new byte[1024 * 256];
            while (_activeSocket.State == WebSocketState.Open)
            {
                var result = await _activeSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
                ProcessRequest(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
        }
        catch { }
        finally
        {
            lock (_lock)
            {
                _activeSocket = null;
            }
            _engine.OnRemoteCommand(() => _engine.CurrentUser = null);
        }
    }

    private async Task RefuseConnection(WebSocket socket)
    {
        try
        {
            var msg = JsonSerializer.Serialize(new { command = "refuse", reason = "App busy" });
            await socket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Busy",
                CancellationToken.None
            );
        }
        catch { }
        finally
        {
            socket.Dispose();
        }
    }

    private void ProcessRequest(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var command = doc.RootElement.GetProperty("command").GetString();

            if (command == "play_level" || command == "edit_level")
            {
                var payload = doc.RootElement.GetProperty("data");
                string xmlData = payload.GetProperty("data").GetString() ?? "";
                int cloudId = payload.GetProperty("cloudId").GetInt32();
                int authorId = payload.TryGetProperty("authorId", out var aProp)
                    ? aProp.GetInt32()
                    : -1;

                _engine.OnRemoteCommand(() =>
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), $"yugo_cloud_{cloudId}.xml");
                    File.WriteAllText(tempPath, xmlData);
                    var identity = new LevelIdentity(tempPath, null, cloudId, authorId);
                    if (command == "play_level")
                        _engine.LoadGameplay(identity);
                    else
                        _engine.LoadEditor(identity);
                });
            }
            else if (command == "sync_cloud_list")
            {
                var data = doc.RootElement.GetProperty("data");
                var titles = new Dictionary<int, string>();
                foreach (var item in data.EnumerateArray())
                {
                    int id = item.GetProperty("id").GetInt32();
                    string title = item.GetProperty("title").GetString() ?? "Cloud Level";
                    titles[id] = title;
                }
                _engine.OnRemoteCommand(() => _engine.SyncCloudTitles(titles));
            }
            else if (command == "set_user")
            {
                var data = doc.RootElement.GetProperty("data").GetString();
                _engine.OnRemoteCommand(() =>
                    _engine.CurrentUser = string.IsNullOrEmpty(data)
                        ? null
                        : JsonSerializer.Deserialize<Engine.UserInfo>(
                            data,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        )
                );
            }
            else if (command == "sync_success")
            {
                int newId = doc.RootElement.GetProperty("cloudId").GetInt32();
                _engine.OnRemoteCommand(() =>
                {
                    if (_engine.CurrentScreen is EditorScreen editor)
                        editor.ConfirmSync(newId);
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Remote] Error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _running = false;
        _listener.Stop();
    }
}
