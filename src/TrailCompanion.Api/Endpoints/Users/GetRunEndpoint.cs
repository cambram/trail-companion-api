using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Application.Modules.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class GetRunEndpoint : IEndpoint
{
    public const string Name = nameof(GetRunEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoints.Users.GetRun,
                async Task<Results<Ok<RaceRunStateDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var state = await sender.Send(new GetRunQuery(appUserId, raceId), cancellationToken);

                    return TypedResults.Ok(state);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Returns a race's live run state, with its arrivals, for the owner or a crew member. Clients poll this.")
            .Produces<RaceRunStateDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
