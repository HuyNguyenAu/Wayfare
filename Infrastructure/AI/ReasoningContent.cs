namespace Wayfare.Infrastructure.AI;

using Microsoft.Extensions.AI;

public sealed class ReasoningContent : AIContent
{
    public ReasoningContent(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Text = text;
    }

    public string Text { get; }
}
