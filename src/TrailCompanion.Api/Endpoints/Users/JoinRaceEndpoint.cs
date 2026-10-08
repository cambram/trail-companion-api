using TrailCompanion.Api.Common.Configuration.DependencyInjection;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class JoinRaceEndpoint : IEndpoint
{
    public const string Name = nameof(JoinRaceEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.JoinRace,
                async Task<Results<Ok<RaceDto>, UnauthorizedHttpResult>> (
                    JoinRaceRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var race = await sender.Send(new JoinRaceCommand(appUserId, request), cancellationToken);

                    return TypedResults.Ok(race);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Joins the authenticated caller to a race's crew by its 6-digit PIN, giving read-only access.")
            .Produces<RaceDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingConfiguration.JoinPolicyName)
            .RequireAuthorization();
    }
}
