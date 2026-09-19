using TrailCompanion.Application.Modules.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class DeleteRaceEndpoint : IEndpoint
{
    public const string Name = nameof(DeleteRaceEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoints.Users.DeleteRace,
                async Task<Results<NoContent, UnauthorizedHttpResult>> (
                    Guid id,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    await sender.Send(new DeleteRaceCommand(appUserId, id), cancellationToken);

                    return TypedResults.NoContent();
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Deletes a race, with its segments and items, owned by the authenticated caller.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
