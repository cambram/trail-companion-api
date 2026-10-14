using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class UpdateRaceEndpoint : IEndpoint
{
    public const string Name = nameof(UpdateRaceEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoints.Users.UpdateRace,
                async Task<Results<Ok<RaceDto>, UnauthorizedHttpResult>> (
                    Guid id,
                    CreateRaceRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var race = await sender.Send(new UpdateRaceCommand(appUserId, id, request), cancellationToken);

                    return TypedResults.Ok(race);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Updates a race's name, carb target, reminder interval, start time and date for the authenticated caller.")
            .Produces<RaceDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
