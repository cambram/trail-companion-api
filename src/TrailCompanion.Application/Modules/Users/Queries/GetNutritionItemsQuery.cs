using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Queries;

public sealed class GetNutritionItemsQuery : IRequest<IReadOnlyList<NutritionItemDto>>
{
    public GetNutritionItemsQuery(Guid appUserId)
    {
        AppUserId = appUserId;
    }

    public Guid AppUserId { get; }
}

internal sealed class GetNutritionItemsQueryHandler
    : IRequestHandler<GetNutritionItemsQuery, IReadOnlyList<NutritionItemDto>>
{
    private readonly INutritionItemsRepository _nutritionItems;

    public GetNutritionItemsQueryHandler(INutritionItemsRepository nutritionItems)
    {
        _nutritionItems = nutritionItems;
    }

    public async Task<IReadOnlyList<NutritionItemDto>> Handle(
        GetNutritionItemsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _nutritionItems.GetAllAsync(request.AppUserId, cancellationToken);

        return items.Select(NutritionItemDto.FromEntity).ToList();
    }
}
