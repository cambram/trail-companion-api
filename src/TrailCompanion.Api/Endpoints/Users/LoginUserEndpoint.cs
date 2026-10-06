using TrailCompanion.Api.Common.Configuration.DependencyInjection;
using TrailCompanion.Api.Common.Services;
using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class LoginUserEndpoint : IEndpoint
{
    public const string Name = nameof(LoginUserEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.LoginUser,
                async (
                    LoginUserRequest request,
                    HttpResponse response,
                    [FromServices] ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new LoginUserCommand(request);
                    var tokens = await sender.Send(command, cancellationToken);
                    RefreshTokenCookie.Write(response, tokens);

                    return TypedResults.Ok(tokens.AccessToken);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Exchanges a username and password for a short-lived bearer token and sets the httpOnly refresh cookie.")
            .Produces<AccessTokenDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingConfiguration.LoginPolicyName)
            .WithMetadata(new RateLimitingConfiguration.PasswordHashingEndpoint())
            .AllowAnonymous();
    }
}
