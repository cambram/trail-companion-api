using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

// ponytail: expired and revoked rows are never deleted; add a scheduled cleanup when the table size matters
public class RefreshTokensRepository : Repository, IRefreshTokensRepository
{
    public RefreshTokensRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<RefreshTokens?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        Context.Set<RefreshTokens>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task AddAsync(RefreshTokens refreshToken, CancellationToken cancellationToken = default) =>
        await AddAsync<RefreshTokens>(refreshToken, cancellationToken);

    public async Task<bool> RotateAsync(
        Guid replacingId,
        RefreshTokens next,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        var revoked = await Context.Set<RefreshTokens>()
            .Where(t => t.Id == replacingId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow)
                    .SetProperty(t => t.ReplacedByTokenId, next.Id)
                    .SetProperty(t => t.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken);

        if (revoked == 0)
        {
            return false;
        }

        await AddAsync<RefreshTokens>(next, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public Task RevokeAllAsync(
        Guid appUserId,
        string? exceptTokenHash,
        CancellationToken cancellationToken = default) =>
        Context.Set<RefreshTokens>()
            .Where(t => t.AppUserId == appUserId && t.RevokedAtUtc == null && t.TokenHash != exceptTokenHash)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow)
                    .SetProperty(t => t.UpdatedAtUtc, DateTime.UtcNow),
                cancellationToken);
}
