using TrailCompanion.Api.Common.Configuration.DependencyInjection;
using TrailCompanion.Api.Common.Services;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class RefreshTokenEndpoint : IEndpoint
{
    public const string Name = nameof(RefreshTokenEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.RefreshToken,
                async (
                    HttpRequest request,
                    HttpResponse response,
                    [FromServices] ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new RefreshAccessTokenCommand(RefreshTokenCookie.Read(request));
                    var tokens = await sender.Send(command, cancellationToken);
                    RefreshTokenCookie.Write(response, tokens);

                    return TypedResults.Ok(tokens.AccessToken);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Rotates the refresh cookie and returns a new bearer token. Reusing a spent refresh token revokes every refresh token for that user.")
            .Produces<AccessTokenDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingConfiguration.RefreshPolicyName)
            .AllowAnonymous();
    }
}
