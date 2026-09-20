using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class UpdateNutritionItemEndpoint : IEndpoint
{
    public const string Name = nameof(UpdateNutritionItemEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoints.Users.UpdateNutritionItem,
                async Task<Results<Ok<NutritionItemDto>, UnauthorizedHttpResult>> (
                    Guid id,
                    NutritionItemRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var command = new UpdateNutritionItemCommand(appUserId, id, request);
                    var item = await sender.Send(command, cancellationToken);

                    return TypedResults.Ok(item);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Replaces a nutrition library item owned by the authenticated caller.")
            .Produces<NutritionItemDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization();
    }
}
