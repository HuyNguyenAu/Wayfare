using System.Text.Json.Serialization;

namespace Wayfare.Core.Models;

#region Base AST Node

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(BranchNode), "branch")]
[JsonDerivedType(typeof(TurnNode), "turn")]
public abstract record HistoryNode
{
    public string Id { get; init; } = Guid.CreateVersion7().ToString();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

#endregion

#region Branch & Turn Nodes

public record BranchNode(
    string Summary,
    List<TurnNode> Turns,
    BranchStatus Status) : HistoryNode;

public record TurnNode(SessionMessage Message) : HistoryNode;

#endregion
