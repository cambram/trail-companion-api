using Template.Infrastructure.Common.Persistence;
using Template.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Template.Infrastructure.Common.Repositories;

public abstract class Repository
{
    protected readonly ApplicationDbContext Context;

    protected Repository(ApplicationDbContext context)
    {
        Context = context;
    }

    protected async Task<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
        where TEntity : class
    {
        await Context.AddAsync(entity, cancellationToken);

        return entity;
    }

    protected void Delete<TEntity>(TEntity entity)
        where TEntity : class
    {
        Context.Remove(entity);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
            && UniqueConstraintConflicts.TryTranslate(postgres.ConstraintName, out var conflict))
        {
            throw new ConflictException(conflict.Code, conflict.Message, exception);
        }
    }
}
