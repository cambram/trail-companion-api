using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Application.Modules.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class GetNutritionItemsEndpoint : IEndpoint
{
    public const string Name = nameof(GetNutritionItemsEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoints.Users.GetNutritionItems,
                async Task<Results<Ok<IReadOnlyList<NutritionItemDto>>, UnauthorizedHttpResult>> (
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var items = await sender.Send(new GetNutritionItemsQuery(appUserId), cancellationToken);

                    return TypedResults.Ok(items);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Lists the authenticated caller's nutrition library, ordered by name.")
            .Produces<IReadOnlyList<NutritionItemDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }
}
