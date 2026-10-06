using TrailCompanion.Api.Common.Configuration.DependencyInjection;
using TrailCompanion.Api.Common.Services;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class DeleteMyUserEndpoint : IEndpoint
{
    public const string Name = nameof(DeleteMyUserEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoints.Users.DeleteMyUser,
                async Task<Results<NoContent, UnauthorizedHttpResult>> (
                    [FromBody] DeleteAccountRequest body,
                    HttpResponse response,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    await sender.Send(new DeleteAccountCommand(appUserId, body), cancellationToken);
                    RefreshTokenCookie.Clear(response);

                    return TypedResults.NoContent();
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Permanently hard-deletes the caller's account with all races, segments, items, nutrition items and refresh tokens. Requires the current password.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingConfiguration.AccountPolicyName)
            .WithMetadata(new RateLimitingConfiguration.PasswordHashingEndpoint())
            .RequireAuthorization();
    }
}
