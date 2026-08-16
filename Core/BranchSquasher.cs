using System.Text;
using Wayfare.Core.Abstractions;
using Wayfare.Core.Models;
using Wayfare.Core.Models.Ast;
using Wayfare.Core.Models.Messages;
using Wayfare.Core.Prompts;

namespace Wayfare.Core;

public class BranchSquasher(IChatClient chatClient) : IBranchSquasher
{
    public async Task<string> SquashAsync(ISession session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.History.Count == 0 || session.History[^1] is not BranchNode activeBranch)
        {
            throw new InvalidOperationException($"Cannot squash active branch because session history does not end with a {nameof(BranchNode)}.");
        }

        StringBuilder userPromptBuilder = new();
        userPromptBuilder.AppendLine("Execution trace:");

        foreach (TurnNode turn in activeBranch.Turns)
        {
            switch (turn.Message)
            {
                case UserMessage userMessage:
                    userPromptBuilder.AppendLine($"User: {userMessage.Content}");
                    break;
                case AssistantMessage assistantMessage:
                    userPromptBuilder.AppendLine($"Assistant: {assistantMessage.Content}");
                    break;
                case ToolCallMessage toolCallMessage:
                    foreach (ToolCall toolCall in toolCallMessage.ToolCalls)
                    {
                        userPromptBuilder.AppendLine($"Tool Call: {toolCall.Name}({toolCall.Arguments})");
                    }
                    break;
                case ToolResultMessage toolResultMessage:
                    foreach (ToolExecutionResult result in toolResultMessage.Results)
                    {
                        string content = result.Success ? result.Result : result.Error;
                        userPromptBuilder.AppendLine($"Tool Result ({result.ToolName}): {content}");
                    }
                    break;
            }
        }

        userPromptBuilder.AppendLine();
        userPromptBuilder.AppendLine("Output Format:");
        userPromptBuilder.AppendLine("Situation: <Context/state before starting this branch>");
        userPromptBuilder.AppendLine("Task: <Specific task or goal>");
        userPromptBuilder.AppendLine("Action: <Steps taken to address the task>");
        userPromptBuilder.AppendLine("Result: <Concrete outcome, produced artifacts, or resolved state>");
        userPromptBuilder.AppendLine("Learnings: <Discovered constraints, failed attempts, or key insights for future steps>");

        IReadOnlyList<SessionMessage> messages = [
            new SystemMessage(SquashPromptBuilder.Build()),
            new UserMessage(userPromptBuilder.ToString()),
        ];

        ChatCompletionResult summary = await chatClient.CompleteChatAsync(messages, [], cancellationToken);

        return summary.Content;
    }
}
