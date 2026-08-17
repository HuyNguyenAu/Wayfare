using System.Text.Json.Serialization;
using Wayfare.Core.Models.Messages;

namespace Wayfare.Core.Models.Ast;

#region Base AST Node

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

#endregion

#region Branch & Turn Nodes

/// <summary>
/// Branch node representing an epoch/sub-goal in conversation history.
/// </summary>
public record BranchNode(
    string Summary,
    List<TurnNode> Turns,
    BranchStatus Status) : HistoryNode;

/// <summary>
/// Turn node representing a discrete message exchange within a branch.
/// </summary>
public record TurnNode(SessionMessage Message) : HistoryNode;

#endregion
