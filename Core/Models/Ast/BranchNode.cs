namespace Wayfare.Core.Models.Ast;

public record BranchNode(string Summary, List<TurnNode> Turns) : HistoryNode;
