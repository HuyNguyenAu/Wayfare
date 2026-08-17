namespace Wayfare.Core.Models.Ast;

using Wayfare.Core.Models;

public record BranchNode(
    string Summary,
    List<TurnNode> Turns,
    BranchStatus Status) : HistoryNode;
