using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface IRefreshTokensRepository
{
    Task<RefreshTokens?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshTokens refreshToken, CancellationToken cancellationToken = default);
    Task<bool> TryRevokeAsync(Guid id, Guid replacedByTokenId, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(Guid appUserId, string? exceptTokenHash, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
