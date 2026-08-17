namespace Wayfare.Core.Abstractions;

using Wayfare.Core.Models.Ast;
using Wayfare.Core.Models.Messages;

public interface IMessagePromptBuilder
{
    IReadOnlyList<SessionMessage> BuildMessages(IReadOnlyList<ITool> tools, IReadOnlyList<HistoryNode> history, string intent);
}
