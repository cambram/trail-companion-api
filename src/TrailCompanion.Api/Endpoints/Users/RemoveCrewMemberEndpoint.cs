using TrailCompanion.Application.Modules.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class RemoveCrewMemberEndpoint : IEndpoint
{
    public const string Name = nameof(RemoveCrewMemberEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoints.Users.RemoveCrewMember,
                async Task<Results<NoContent, UnauthorizedHttpResult>> (
                    Guid raceId,
                    Guid memberId,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    await sender.Send(new RemoveCrewMemberCommand(appUserId, raceId, memberId), cancellationToken);

                    return TypedResults.NoContent();
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Removes a crew member; the owner can remove anyone and a crew member can remove themselves.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
