using TrailCompanion.Api.Common.Configuration.DependencyInjection;
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
                    [FromServices] ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new LoginUserCommand(request);
                    var accessToken = await sender.Send(command, cancellationToken);

                    return TypedResults.Ok(accessToken);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Exchanges an email and password for a short-lived bearer token.")
            .Produces<AccessTokenDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .RequireRateLimiting(RateLimitingConfiguration.LoginPolicyName)
            .AllowAnonymous();
    }
}
