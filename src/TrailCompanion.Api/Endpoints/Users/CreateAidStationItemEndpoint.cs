using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class CreateAidStationItemEndpoint : IEndpoint
{
    public const string Name = nameof(CreateAidStationItemEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.CreateAidStationItem,
                async Task<Results<Created<AidStationItemDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    Guid aidStationId,
                    ItemRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var item = await sender.Send(
                        new CreateAidStationItemCommand(appUserId, raceId, aidStationId, request), cancellationToken);
                    var location = ApiEndpoints.Users.DeleteAidStationItem
                        .Replace("{raceId:guid}", raceId.ToString())
                        .Replace("{aidStationId:guid}", aidStationId.ToString())
                        .Replace("{id:guid}", item.Id.ToString());

                    return TypedResults.Created(location, item);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Attaches a nutrition library item to an aid station of a race owned by the authenticated caller.")
            .Produces<AidStationItemDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
