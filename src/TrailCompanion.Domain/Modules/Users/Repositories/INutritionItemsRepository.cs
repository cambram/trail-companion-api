using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Domain.Modules.Users.Repositories;

public interface INutritionItemsRepository
{
    Task<NutritionItems?> GetByIdAsync(Guid appUserId, Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NutritionItems>> GetAllAsync(Guid appUserId, CancellationToken cancellationToken = default);
    Task<int> CountUsagesAsync(Guid appUserId, Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(NutritionItems nutritionItem, CancellationToken cancellationToken = default);
    void Delete(NutritionItems nutritionItem);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
