using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class ReorderItemsEndpoint : IEndpoint
{
    public const string Name = nameof(ReorderItemsEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoints.Users.ReorderItems,
                async Task<Results<NoContent, UnauthorizedHttpResult>> (
                    Guid raceId,
                    Guid segmentId,
                    ReorderRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    await sender.Send(new ReorderItemsCommand(appUserId, raceId, segmentId, request), cancellationToken);

                    return TypedResults.NoContent();
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Sets item order on a segment owned by the authenticated caller from a complete list of ids.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
