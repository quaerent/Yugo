using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Yugo.Server.Services;

public class RemoteAppService
{
    private const int AppPort = 9090;
    private readonly ConcurrentQueue<PendingCommand> _queue = new();
    private readonly CancellationTokenSource _cts = new();

    public RemoteAppService()
    {
        Task.Run(ProcessQueueAsync);
    }

    public void EnqueueCommand(string command, string data)
    {
        _queue.Enqueue(new PendingCommand(command, data));
        Console.WriteLine($"[RemoteService] Command '{command}' queued for retry.");
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (_queue.TryPeek(out var cmd))
            {
                bool success = await SendCommandInternalAsync("127.0.0.1", cmd.Command, cmd.Data);
                if (success)
                {
                    _queue.TryDequeue(out _);
                    Console.WriteLine(
                        $"[RemoteService] Command '{cmd.Command}' delivered successfully."
                    );
                }
                else
                {
                    // Wait before retrying
                    await Task.Delay(5000);
                }
            }
            else
            {
                await Task.Delay(1000);
            }
        }
    }

    private async Task<bool> SendCommandInternalAsync(string ip, string command, string data)
    {
        try
        {
            using var client = new TcpClient();
            // Use a short timeout for the connection attempt
            var connectTask = client.ConnectAsync(ip, AppPort);
            if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                return false;

            using var stream = client.GetStream();
            var request = new { command = command, data = data };
            var json = JsonSerializer.Serialize(request);
            var bytes = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private record PendingCommand(string Command, string Data);
}
