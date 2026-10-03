using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class CreateAidStationEndpoint : IEndpoint
{
    public const string Name = nameof(CreateAidStationEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.CreateAidStation,
                async Task<Results<Created<AidStationDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    CreateAidStationRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var station = await sender.Send(new CreateAidStationCommand(appUserId, raceId, request), cancellationToken);
                    var location = ApiEndpoints.Users.UpdateAidStation
                        .Replace("{raceId:guid}", raceId.ToString())
                        .Replace("{id:guid}", station.Id.ToString());

                    return TypedResults.Created(location, station);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Adds an aid station before a segment of a race owned by the authenticated caller.")
            .Produces<AidStationDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization();
    }
}
