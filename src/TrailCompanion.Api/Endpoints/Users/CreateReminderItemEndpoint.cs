using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Endpoints.Users;

internal sealed class CreateReminderItemEndpoint : IEndpoint
{
    public const string Name = nameof(CreateReminderItemEndpoint);

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Users.CreateReminderItem,
                async Task<Results<Created<ReminderItemDto>, UnauthorizedHttpResult>> (
                    Guid raceId,
                    int number,
                    ItemRequest request,
                    [FromServices] ISender sender,
                    [FromServices] ICurrentUser currentUser,
                    CancellationToken cancellationToken) =>
                {
                    if (currentUser.AppUserId is not { } appUserId)
                    {
                        return TypedResults.Unauthorized();
                    }

                    var item = await sender.Send(
                        new CreateReminderItemCommand(appUserId, raceId, number, request), cancellationToken);
                    var location = ApiEndpoints.Users.DeleteReminderItem
                        .Replace("{raceId:guid}", raceId.ToString())
                        .Replace("{number:int}", number.ToString())
                        .Replace("{id:guid}", item.Id.ToString());

                    return TypedResults.Created(location, item);
                })
            .WithName(Name)
            .WithTags(Modules.Users)
            .WithSummary("Attaches a nutrition library item to a reminder of a race owned by the authenticated caller.")
            .Produces<ReminderItemDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();
    }
}
