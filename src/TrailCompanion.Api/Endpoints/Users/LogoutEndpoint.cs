using TrailCompanion.Api.Common.Services;
using TrailCompanion.Application.Modules.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class LogoutEndpoint : IEndpoint
{
    public const string Name = nameof(LogoutEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.Logout,
                async (
                    HttpRequest request,
                    HttpResponse response,
                    [FromServices] ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    await sender.Send(new LogoutCommand(RefreshTokenCookie.Read(request)), cancellationToken);
                    RefreshTokenCookie.Clear(response);

                    return TypedResults.NoContent();
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Revokes the presented refresh cookie and clears it. Works with an expired bearer token.")
            .Produces(StatusCodes.Status204NoContent)
            .AllowAnonymous();
    }
}
