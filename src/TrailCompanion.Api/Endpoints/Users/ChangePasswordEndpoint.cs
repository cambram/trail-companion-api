using TrailCompanion.Api.Common.Configuration.DependencyInjection;
using TrailCompanion.Api.Common.Services;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class ChangePasswordEndpoint : IEndpoint
{
    public const string Name = nameof(ChangePasswordEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.ChangePassword,
                async Task<Results<NoContent, UnauthorizedHttpResult>> (
                    ChangePasswordRequest body,
                    HttpRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var command = new ChangePasswordCommand(appUserId, body, RefreshTokenCookie.Read(request));
                    await sender.Send(command, cancellationToken);

                    return TypedResults.NoContent();
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Changes the caller's password and revokes their other refresh tokens. Issued bearer tokens stay valid until they expire.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingConfiguration.AccountPolicyName)
            .WithMetadata(new RateLimitingConfiguration.PasswordHashingEndpoint())
            .RequireAuthorization();
    }
}
