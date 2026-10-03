using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using TrailCompanion.Infrastructure.Common.Repositories;
using TrailCompanion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TrailCompanion.Infrastructure.Repositories.Users;

public class NutritionItemsRepository : Repository, INutritionItemsRepository
{
    public NutritionItemsRepository(ApplicationDbContext context) : base(context)
    {
    }

    public Task<NutritionItems?> GetByIdAsync(
        Guid appUserId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Context.Set<NutritionItems>()
            .FirstOrDefaultAsync(n => n.Id == id && n.AppUserId == appUserId, cancellationToken);

    public async Task<IReadOnlyList<NutritionItems>> GetAllAsync(
        Guid appUserId,
        CancellationToken cancellationToken = default) =>
        await Context.Set<NutritionItems>()
            .AsNoTracking()
            .Where(n => n.AppUserId == appUserId)
            .OrderBy(n => n.Name)
            .ToListAsync(cancellationToken);

    public async Task<int> CountUsagesAsync(
        Guid appUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var owned = Context.Set<NutritionItems>().Where(n => n.Id == id && n.AppUserId == appUserId);

        var inSegments = await Context.Set<Items>()
            .CountAsync(i => i.NutritionItemId == id && owned.Any(), cancellationToken);
        var atStations = await Context.Set<AidStationItems>()
            .CountAsync(i => i.NutritionItemId == id && owned.Any(), cancellationToken);

        return inSegments + atStations;
    }

    public async Task AddAsync(NutritionItems nutritionItem, CancellationToken cancellationToken = default) =>
        await AddAsync<NutritionItems>(nutritionItem, cancellationToken);

    public void Delete(NutritionItems nutritionItem) => Delete<NutritionItems>(nutritionItem);
}
