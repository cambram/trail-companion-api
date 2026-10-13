using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class StartRunEndpoint : IEndpoint
{
    public const string Name = nameof(StartRunEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.StartRun,
                async Task<Results<Ok<RaceRunStateDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    StartRunRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var state = await sender.Send(new StartRunCommand(appUserId, raceId, request), cancellationToken);

                    return TypedResults.Ok(state);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Starts live tracking on race day. The owner and crew may both start. 409 when a run already exists.")
            .Produces<RaceRunStateDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization();
    }
}
