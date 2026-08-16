namespace Wayfare.Core.Abstractions;

public interface IBranchSquasher
{
    Task<string> SquashAsync(ISession session, CancellationToken cancellationToken);
}
