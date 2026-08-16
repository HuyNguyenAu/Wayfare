namespace Wayfare.Core.Models.Ast;

using System.Text.Json.Serialization;

/// <summary>
/// Base AST node for conversation history.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BranchNode), "branch")]
[JsonDerivedType(typeof(TurnNode), "turn")]
public abstract record HistoryNode
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
