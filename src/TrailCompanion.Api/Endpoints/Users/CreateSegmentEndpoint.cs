using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class CreateSegmentEndpoint : IEndpoint
{
    public const string Name = nameof(CreateSegmentEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.CreateSegment,
                async Task<Results<Created<SegmentDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    SegmentRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var command = new CreateSegmentCommand(appUserId, raceId, request);
                    var segment = await sender.Send(command, cancellationToken);
                    var location = ApiEndpoints.Users.UpdateSegment
                        .Replace("{raceId:guid}", raceId.ToString())
                        .Replace("{id:guid}", segment.Id.ToString());

                    return TypedResults.Created(location, segment);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Appends a segment to a race owned by the authenticated caller.")
            .Produces<SegmentDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
