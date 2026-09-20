using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class CreateNutritionItemEndpoint : IEndpoint
{
    public const string Name = nameof(CreateNutritionItemEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.CreateNutritionItem,
                async Task<Results<Created<NutritionItemDto>, UnauthorizedHttpResult>> (
                    NutritionItemRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var item = await sender.Send(new CreateNutritionItemCommand(appUserId, request), cancellationToken);
                    var location = ApiEndpoints.Users.UpdateNutritionItem
                        .Replace("{id:guid}", item.Id.ToString());

                    return TypedResults.Created(location, item);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Creates a nutrition library item for the authenticated caller.")
            .Produces<NutritionItemDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization();
    }
}
