using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Application.Modules.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class GetRaceEndpoint : IEndpoint
{
    public const string Name = nameof(GetRaceEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoints.Users.GetRace,
                async Task<Results<Ok<RaceDetailDto>, UnauthorizedHttpResult>> (
                    Guid id,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var race = await sender.Send(new GetRaceQuery(appUserId, id), cancellationToken);

                    return TypedResults.Ok(race);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Gets a race owned by the authenticated caller, with its segments and their items.")
            .Produces<RaceDetailDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
