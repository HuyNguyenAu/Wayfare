namespace Wayfare.Core.Models.Ast;

using Wayfare.Core.Models.Messages;

public record TurnNode(SessionMessage Message) : HistoryNode;
