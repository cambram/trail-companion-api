using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class UpdateSegmentEndpoint : IEndpoint
{
    public const string Name = nameof(UpdateSegmentEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoints.Users.UpdateSegment,
                async Task<Results<Ok<SegmentDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    Guid id,
                    SegmentRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var command = new UpdateSegmentCommand(appUserId, raceId, id, request);
                    var segment = await sender.Send(command, cancellationToken);

                    return TypedResults.Ok(segment);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Replaces a segment's editable fields on a race owned by the authenticated caller.")
            .Produces<SegmentDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
