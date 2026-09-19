using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class CreateItemEndpoint : IEndpoint
{
    public const string Name = nameof(CreateItemEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.CreateItem,
                async Task<Results<Created<ItemDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    Guid segmentId,
                    ItemRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var command = new CreateItemCommand(appUserId, raceId, segmentId, request);
                    var item = await sender.Send(command, cancellationToken);
                    var location = ApiEndpoints.Users.UpdateItem
                        .Replace("{raceId:guid}", raceId.ToString())
                        .Replace("{segmentId:guid}", segmentId.ToString())
                        .Replace("{id:guid}", item.Id.ToString());

                    return TypedResults.Created(location, item);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Appends an item to a segment of a race owned by the authenticated caller.")
            .Produces<ItemDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
