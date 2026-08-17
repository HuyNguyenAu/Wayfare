namespace Wayfare.Agent;

using System.Text;
using Wayfare.Infrastructure.AI;
using Wayfare.Session;
using Wayfare.Tools;

public class BranchSquasher(IChatClient chatClient) : IBranchSquasher
{
    #region Public API

    public async Task<string> SquashAsync(ISession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.History.Count == 0 || session.History[^1] is not BranchNode activeBranch)
        {
            throw new InvalidOperationException($"Cannot squash active branch because session history does not end with a {nameof(BranchNode)}.");
        }

        IReadOnlyList<SessionMessage> messages = [
            new SystemMessage(SquashPromptBuilder.Build()),
            new UserMessage(BuildTraceString(activeBranch))
        ];

        ChatCompletionResult summary = await chatClient.CompleteChatAsync(messages, [], cancellationToken);
        return summary.Content.Trim();
    }

    #endregion

    #region Internal Trace Formatting Helpers

    private static string BuildTraceString(BranchNode activeBranch)
    {
        StringBuilder traceBuilder = new();
        traceBuilder.AppendLine("Execution trace:");

        foreach (TurnNode turn in activeBranch.Turns)
        {
            switch (turn.Message)
            {
                case UserMessage userMessage:
                    traceBuilder.AppendLine($"User: {userMessage.Content}");
                    break;
                case AssistantMessage assistantMessage:
                    traceBuilder.AppendLine($"Assistant: {assistantMessage.Content}");
                    break;
                case ToolCallMessage toolCallMessage:
                    foreach (ToolCall toolCall in toolCallMessage.ToolCalls)
                    {
                        traceBuilder.AppendLine($"Tool Call: {toolCall.Name}({toolCall.Arguments})");
                    }
                    break;
                case ToolResultMessage toolResultMessage:
                    foreach (ToolExecutionResult result in toolResultMessage.Results)
                    {
                        string content = result.Success ? result.Result : result.Error;
                        traceBuilder.AppendLine($"Tool Result ({result.ToolName}): {content}");
                    }
                    break;
            }
        }

        traceBuilder.AppendLine();
        traceBuilder.AppendLine("Output Format:");
        traceBuilder.AppendLine("Situation: <Context/state before starting this branch>");
        traceBuilder.AppendLine("Task: <Specific task or goal>");
        traceBuilder.AppendLine("Action: <Steps taken to address the task>");
        traceBuilder.AppendLine("Result: <Concrete outcome, produced artifacts, or resolved state>");
        traceBuilder.AppendLine("Learnings: <Discovered constraints, failed attempts, or key insights for future steps>");

        return traceBuilder.ToString();
    }

    #endregion
}
