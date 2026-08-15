using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Models.Messages;

namespace Wayfare.Persistence;

public class SessionStore : ISessionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public string SessionsDirectory { get; }
    public string SessionId { get; }
    public string CurrentFilePath { get; }

    public SessionStore(string sessionsDirectory)
    {
        if (string.IsNullOrWhiteSpace(sessionsDirectory))
        {
            throw new ArgumentException("Sessions directory must be specified.", nameof(sessionsDirectory));
        }

        SessionsDirectory = sessionsDirectory;
        Directory.CreateDirectory(SessionsDirectory);
        SessionId = $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        CurrentFilePath = Path.Combine(SessionsDirectory, $"{SessionId}.jsonl");
    }

    public async Task AppendMessageAsync(SessionMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            string json = JsonSerializer.Serialize(message, SerializerOptions);

            await using FileStream stream = new(
                CurrentFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite,
                bufferSize: 4096,
                useAsync: true);

            await using StreamWriter writer = new(stream, Encoding.UTF8);
            await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            // Handle IO/serialization errors gracefully without throwing unhandled exceptions
        }
    }

    public async Task<IReadOnlyList<SessionMessage>> LoadMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(CurrentFilePath))
        {
            return [];
        }

        List<SessionMessage> messages = [];

        try
        {
            await using FileStream stream = new(
                CurrentFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 4096,
                useAsync: true);

            using StreamReader reader = new(stream, Encoding.UTF8);
            string? line;

            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    SessionMessage? message = JsonSerializer.Deserialize<SessionMessage>(line, SerializerOptions);
                    if (message is not null)
                    {
                        messages.Add(message);
                    }
                }
                catch (JsonException)
                {
                    // Gracefully skip corrupted or malformed lines
                }
            }
        }
        catch (IOException)
        {
            // Gracefully handle IO exceptions on file read
        }

        return messages.AsReadOnly();
    }
}
