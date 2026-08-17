namespace Wayfare.UI;

public interface ITerminalUI
{
    Task<string> GetUserInputAsync(CancellationToken cancellationToken);
    Task WaitForCompletionAsync();
}
