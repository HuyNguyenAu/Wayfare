namespace Wayfare.Core.Abstractions;

public interface IIntentResolver
{
    Task<string> ResolveAsync(string currentIntent, string userInput, CancellationToken cancellationToken);
}
