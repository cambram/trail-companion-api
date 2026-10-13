using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class EditArrivalEndpoint : IEndpoint
{
    public const string Name = nameof(EditArrivalEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoints.Users.EditArrival,
                async Task<Results<Ok<RaceRunStateDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    string checkpoint,
                    EditArrivalRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var state = await sender.Send(
                        new EditArrivalCommand(appUserId, raceId, checkpoint, request), cancellationToken);

                    return TypedResults.Ok(state);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Corrects a recorded arrival time. The new time must stay after the start and in checkpoint order.")
            .Produces<RaceRunStateDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
