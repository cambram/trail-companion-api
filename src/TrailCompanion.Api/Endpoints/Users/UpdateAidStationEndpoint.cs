using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class UpdateAidStationEndpoint : IEndpoint
{
    public const string Name = nameof(UpdateAidStationEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoints.Users.UpdateAidStation,
                async Task<Results<Ok<AidStationDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    Guid id,
                    AidStationRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var station = await sender.Send(new UpdateAidStationCommand(appUserId, raceId, id, request), cancellationToken);

                    return TypedResults.Ok(station);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Replaces an aid station's name, notes and kit list on a race owned by the authenticated caller.")
            .Produces<AidStationDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
