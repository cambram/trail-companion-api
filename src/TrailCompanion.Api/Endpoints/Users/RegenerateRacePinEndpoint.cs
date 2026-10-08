using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class RegenerateRacePinEndpoint : IEndpoint
{
    public const string Name = nameof(RegenerateRacePinEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.RegenerateRacePin,
                async Task<Results<Ok<RacePinDto>, UnauthorizedHttpResult>> (
                    Guid id,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var pin = await sender.Send(new RegenerateRacePinCommand(appUserId, id), cancellationToken);

                    return TypedResults.Ok(pin);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Replaces an owned race's join PIN; the old PIN stops working.")
            .Produces<RacePinDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
