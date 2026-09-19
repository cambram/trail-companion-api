using TrailCompanion.Application.Modules.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class DeleteItemEndpoint : IEndpoint
{
    public const string Name = nameof(DeleteItemEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoints.Users.DeleteItem,
                async Task<Results<NoContent, UnauthorizedHttpResult>> (
                    Guid raceId,
                    Guid segmentId,
                    Guid id,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    await sender.Send(new DeleteItemCommand(appUserId, raceId, segmentId, id), cancellationToken);

                    return TypedResults.NoContent();
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Deletes an item from a segment of a race owned by the authenticated caller.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
