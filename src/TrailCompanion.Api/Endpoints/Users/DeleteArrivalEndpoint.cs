using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class DeleteArrivalEndpoint : IEndpoint
{
    public const string Name = nameof(DeleteArrivalEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoints.Users.DeleteArrival,
                async Task<Results<Ok<RaceRunStateDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    string checkpoint,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var state = await sender.Send(new DeleteArrivalCommand(appUserId, raceId, checkpoint), cancellationToken);

                    return TypedResults.Ok(state);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Undoes a recorded arrival. Undoing the finish reopens the run.")
            .Produces<RaceRunStateDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
