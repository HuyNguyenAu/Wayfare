using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Models;

namespace Wayfare.Persistence;

public class SessionStore : ISessionStore
{
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    private readonly string _currentFilePath;
    private readonly ISession _session = new Session();
    private readonly Channel<byte> _writeChannel;
    private readonly Task _backgroundWriteTask;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public SessionStore(string sessionsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionsDirectory);

        Directory.CreateDirectory(sessionsDirectory);

        _currentFilePath = Path.Combine(sessionsDirectory, $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        _writeChannel = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });
        _backgroundWriteTask = Task.Run(ProcessWriteQueueAsync);
    }

    public ISession Session => _session;

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        _writeChannel.Writer.TryWrite(0);

        return Task.CompletedTask;
    }

    private async Task ProcessWriteQueueAsync()
    {
        while (await _writeChannel.Reader.WaitToReadAsync(_cancellationTokenSource.Token))
        {
            while (_writeChannel.Reader.TryRead(out _)) { }

            try
            {
                await WriteSessionToFileAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (_cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SessionStore] Background save failed: {ex.Message}");
            }
        }
    }

    private async Task WriteSessionToFileAsync(CancellationToken cancellationToken)
    {
        string json;

        lock (_session)
        {
            json = JsonSerializer.Serialize(_session.History, _serializerOptions);
        }

        string tempFilePath = $"{_currentFilePath}.tmp";
        await File.WriteAllTextAsync(tempFilePath, json, Encoding.UTF8, cancellationToken);
        File.Move(tempFilePath, _currentFilePath, overwrite: true);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        _writeChannel.Writer.TryComplete();

        try
        {
            await _backgroundWriteTask;
        }
        catch (Exception)
        {
            // Ignore cancellation on background task shutdown
        }

        try
        {
            await WriteSessionToFileAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SessionStore] Final save failed during disposal: {ex.Message}");
        }

        _cancellationTokenSource.Dispose();
    }
}

