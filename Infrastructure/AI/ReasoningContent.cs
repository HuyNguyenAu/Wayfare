namespace Wayfare.Infrastructure.AI;

using Microsoft.Extensions.AI;

public sealed class ReasoningContent(string text) : AIContent
{
    public string Text { get; } = text ?? throw new ArgumentNullException(nameof(text));
}
