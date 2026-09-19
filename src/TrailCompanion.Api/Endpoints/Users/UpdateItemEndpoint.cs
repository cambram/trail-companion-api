using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class UpdateItemEndpoint : IEndpoint
{
    public const string Name = nameof(UpdateItemEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoints.Users.UpdateItem,
                async Task<Results<Ok<ItemDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    Guid segmentId,
                    Guid id,
                    ItemRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var command = new UpdateItemCommand(appUserId, raceId, segmentId, id, request);
                    var item = await sender.Send(command, cancellationToken);

                    return TypedResults.Ok(item);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Replaces an item's editable fields on a segment of a race owned by the authenticated caller.")
            .Produces<ItemDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
