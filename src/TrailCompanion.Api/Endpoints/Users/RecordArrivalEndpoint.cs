using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class RecordArrivalEndpoint : IEndpoint
{
    public const string Name = nameof(RecordArrivalEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.RecordArrival,
                async Task<Results<Ok<RaceRunStateDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    RecordArrivalRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var state = await sender.Send(new RecordArrivalCommand(appUserId, raceId, request), cancellationToken);

                    return TypedResults.Ok(state);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Records the athlete at an aid station or the finish. 409 when that checkpoint is already recorded.")
            .Produces<RaceRunStateDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization();
    }
}
