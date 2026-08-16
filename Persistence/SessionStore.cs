using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public SessionStore(string sessionsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionsDirectory);

        Directory.CreateDirectory(sessionsDirectory);

        _currentFilePath = Path.Combine(sessionsDirectory, $"session_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
    }

    public ISession Session => _session;

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(Session.History, _serializerOptions);

        string tempFilePath = $"{_currentFilePath}.tmp";
        await File.WriteAllTextAsync(tempFilePath, json, Encoding.UTF8, cancellationToken);
        File.Move(tempFilePath, _currentFilePath, overwrite: true);
    }
}
