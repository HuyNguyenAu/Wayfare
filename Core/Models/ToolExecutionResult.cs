using System.Text.Json.Serialization;

namespace Wayfare.Core.Models;

public sealed record ToolExecutionResult(
    bool Success,
    string DisplayMessage,
    string Result,
    string Error,
    [property: JsonIgnore] Exception? Exception = null,
    string ToolId = "",
    string ToolName = "");
