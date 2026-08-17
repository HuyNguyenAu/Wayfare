namespace Wayfare.Session.Transformations;

using System.Text.Json;
using System.Text.RegularExpressions;
using Wayfare.Infrastructure.AI;
using Wayfare.Session;
using Wayfare.Tools;

public sealed class ResourceIndex : IResourceIndex
{
    private static readonly Regex _pathAttributeRegex = new(@"path=""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> _writeTools = new(StringComparer.OrdinalIgnoreCase) { "write", "replace" };
    private static readonly HashSet<string> _readTools = new(StringComparer.OrdinalIgnoreCase) { "read", "read_file" };

    private readonly List<ResourceAccessRecord> _records = [];
    private readonly Dictionary<string, List<ResourceAccessRecord>> _indexByResource = new(StringComparer.OrdinalIgnoreCase);

    public void IndexNode(HistoryNode node, int trunkIndex)
    {
        ArgumentNullException.ThrowIfNull(node);

        List<ResourceAccessRecord> extractedRecords = ExtractAccessRecords(node, trunkIndex);
        AddRecords(extractedRecords);
    }

    public IReadOnlyList<ResourceAccessRecord> GetAccessHistory(string resourceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceKey);

        return _indexByResource.TryGetValue(resourceKey, out List<ResourceAccessRecord>? records)
            ? records.AsReadOnly()
            : [];
    }

    public IReadOnlyList<ResourceAccessRecord> GetAllRecords() => _records.AsReadOnly();

    public void RebuildIndex(IReadOnlyList<HistoryNode> trunk)
    {
        ArgumentNullException.ThrowIfNull(trunk);

        Clear();

        for (int index = 0; index < trunk.Count; index++)
        {
            List<ResourceAccessRecord> records = ExtractAccessRecords(trunk[index], index, trunk);
            AddRecords(records);
        }
    }

    public void Clear()
    {
        _records.Clear();
        _indexByResource.Clear();
    }

    private void AddRecords(IEnumerable<ResourceAccessRecord> records)
    {
        foreach (ResourceAccessRecord record in records)
        {
            _records.Add(record);

            if (!_indexByResource.TryGetValue(record.ResourceKey, out List<ResourceAccessRecord>? resourceList))
            {
                resourceList = [];
                _indexByResource[record.ResourceKey] = resourceList;
            }

            resourceList.Add(record);
        }
    }

    public static List<ResourceAccessRecord> ExtractAccessRecords(HistoryNode node, int trunkIndex, IReadOnlyList<HistoryNode>? trunk = null)
    {
        ArgumentNullException.ThrowIfNull(node);

        SessionMessage message = node.ToProjectedMessage();

        return message switch
        {
            ToolCallMessage toolCallMessage => ExtractFromToolCalls(toolCallMessage, node.Id, trunkIndex, node.CreatedAt),
            ToolResultMessage toolResultMessage => ExtractFromToolResults(toolResultMessage, node.Id, trunkIndex, node.CreatedAt, trunk),
            _ => []
        };
    }

    private static List<ResourceAccessRecord> ExtractFromToolCalls(ToolCallMessage message, string nodeId, int trunkIndex, DateTime createdAt)
    {
        List<ResourceAccessRecord> records = [];

        foreach (ToolCall toolCall in message.ToolCalls)
        {
            if (TryExtractPathFromArguments(toolCall.Arguments, out string? path) && !string.IsNullOrWhiteSpace(path))
            {
                ResourceAccessType accessType = IsWriteTool(toolCall.Name) ? ResourceAccessType.Write : ResourceAccessType.Read;
                records.Add(new ResourceAccessRecord(path, nodeId, trunkIndex, accessType, createdAt));
            }
        }

        return records;
    }

    private static List<ResourceAccessRecord> ExtractFromToolResults(ToolResultMessage message, string nodeId, int trunkIndex, DateTime createdAt, IReadOnlyList<HistoryNode>? trunk)
    {
        List<ResourceAccessRecord> records = [];

        foreach (ToolExecutionResult result in message.Results)
        {
            if (!TryExtractPathFromExecutionResult(result, out string? path) || string.IsNullOrWhiteSpace(path))
            {
                if (trunk is not null && trunkIndex > 0 &&
                    trunk[trunkIndex - 1].ToProjectedMessage() is ToolCallMessage precedingCall)
                {
                    ToolCall? matchingCall = precedingCall.ToolCalls.FirstOrDefault(c =>
                        (!string.IsNullOrEmpty(result.ToolId) && c.ToolId == result.ToolId) ||
                        c.Name.Equals(result.ToolName, StringComparison.OrdinalIgnoreCase));

                    if (matchingCall is not null && TryExtractPathFromArguments(matchingCall.Arguments, out string? callPath))
                    {
                        path = callPath;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                ResourceAccessType accessType = IsWriteTool(result.ToolName) ? ResourceAccessType.Write : ResourceAccessType.Read;
                records.Add(new ResourceAccessRecord(path, nodeId, trunkIndex, accessType, createdAt));
            }
        }

        return records;
    }

    public static bool IsWriteTool(string toolName) => _writeTools.Contains(toolName);

    public static bool IsReadTool(string toolName) => _readTools.Contains(toolName);

    public static bool TryExtractPathFromArguments(string? arguments, out string? path)
    {
        path = null;

        if (string.IsNullOrWhiteSpace(arguments))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(arguments);

            if (document.RootElement.TryGetProperty("path", out JsonElement pathElement))
            {
                path = pathElement.GetString();
                return !string.IsNullOrWhiteSpace(path);
            }
        }
        catch
        {
            // Non-JSON or malformed arguments
        }

        return false;
    }

    public static bool TryExtractPathFromExecutionResult(ToolExecutionResult result, out string? path)
    {
        path = null;

        if (!string.IsNullOrWhiteSpace(result.Result))
        {
            Match match = _pathAttributeRegex.Match(result.Result);
            if (match.Success)
            {
                path = match.Groups[1].Value;
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(result.DisplayMessage))
        {
            int singleQuoteStart = result.DisplayMessage.IndexOf('\'');

            if (singleQuoteStart >= 0)
            {
                int singleQuoteEnd = result.DisplayMessage.IndexOf('\'', singleQuoteStart + 1);

                if (singleQuoteEnd > singleQuoteStart)
                {
                    path = result.DisplayMessage.Substring(singleQuoteStart + 1, singleQuoteEnd - singleQuoteStart - 1);
                    return true;
                }
            }
        }

        return false;
    }
}
