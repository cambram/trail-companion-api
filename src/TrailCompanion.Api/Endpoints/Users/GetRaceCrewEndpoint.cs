using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Application.Modules.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class GetRaceCrewEndpoint : IEndpoint
{
    public const string Name = nameof(GetRaceCrewEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoints.Users.GetRaceCrew,
                async Task<Results<Ok<IReadOnlyList<CrewMemberDto>>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var crew = await sender.Send(new GetRaceCrewQuery(appUserId, raceId), cancellationToken);

                    return TypedResults.Ok(crew);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Lists a race's crew for its owner or one of its crew members.")
            .Produces<IReadOnlyList<CrewMemberDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
