using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class CreateRaceEndpoint : IEndpoint
{
    public const string Name = nameof(CreateRaceEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.CreateRace,
                async Task<Results<Created<RaceDto>, UnauthorizedHttpResult>> (
                    CreateRaceRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var command = new CreateRaceCommand(appUserId, request);
                    var race = await sender.Send(command, cancellationToken);

                    return TypedResults.Created(ApiEndpoints.Users.CreateRace, race);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Creates a race owned by the authenticated caller.")
            .Produces<RaceDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }
}
