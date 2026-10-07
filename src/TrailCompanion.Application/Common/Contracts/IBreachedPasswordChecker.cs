namespace TrailCompanion.Application.Common.Contracts;

public interface IBreachedPasswordChecker
{
    Task<bool> IsBreachedAsync(string password, CancellationToken cancellationToken);
}
