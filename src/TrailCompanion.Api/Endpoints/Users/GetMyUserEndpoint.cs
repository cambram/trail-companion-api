using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Application.Modules.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class GetMyUserEndpoint : IEndpoint
{
    public const string Name = nameof(GetMyUserEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoints.Users.GetMyUser,
                async Task<Results<Ok<AppUserDto>, UnauthorizedHttpResult>> (
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var query = new GetCurrentAppUserQuery(appUserId);
                    var appUser = await sender.Send(query, cancellationToken);

                    return TypedResults.Ok(appUser);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Returns the authenticated caller's account.")
            .Produces<AppUserDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();
    }
}
