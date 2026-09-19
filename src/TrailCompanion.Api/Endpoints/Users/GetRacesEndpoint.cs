using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Application.Modules.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class GetRacesEndpoint : IEndpoint
{
    public const string Name = nameof(GetRacesEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoints.Users.GetRaces,
                async Task<Results<Ok<IReadOnlyList<RaceDto>>, UnauthorizedHttpResult>> (
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var query = new GetRacesQuery(appUserId);
                    var races = await sender.Send(query, cancellationToken);

                    return TypedResults.Ok(races);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Lists the authenticated caller's races.")
            .Produces<IReadOnlyList<RaceDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }
}
