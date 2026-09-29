using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class UpdateMyUserEndpoint : IEndpoint
{
    public const string Name = nameof(UpdateMyUserEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoints.Users.UpdateMyUser,
                async Task<Results<Ok<AppUserDto>, UnauthorizedHttpResult>> (
                    UpdateProfileRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var appUser = await sender.Send(new UpdateProfileCommand(appUserId, request), cancellationToken);

                    return TypedResults.Ok(appUser);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Sets or clears the authenticated caller's first and last name. The email cannot be changed.")
            .Produces<AppUserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }
}
