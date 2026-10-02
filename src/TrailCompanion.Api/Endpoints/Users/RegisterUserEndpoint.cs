using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class RegisterUserEndpoint : IEndpoint
{
    public const string Name = nameof(RegisterUserEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.RegisterUser,
                async (
                    RegisterUserRequest request,
                    [FromServices] ISender sender,
                    CancellationToken cancellationToken) =>
                {
                    var command = new RegisterUserCommand(request);
                    var appUser = await sender.Send(command, cancellationToken);

                    return TypedResults.Created(ApiEndpoints.Users.GetMyUser, appUser);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Creates an account from an email and password.")
            .Produces<AppUserDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AllowAnonymous();
    }
}
