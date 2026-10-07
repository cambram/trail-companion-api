using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class CreateAidStationItemCommand : IRequest<AidStationItemDto>
{
    public CreateAidStationItemCommand(Guid appUserId, Guid raceId, Guid aidStationId, ItemRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        AidStationId = aidStationId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid AidStationId { get; }
    public ItemRequest Request { get; }
}

internal sealed class CreateAidStationItemCommandHandler : IRequestHandler<CreateAidStationItemCommand, AidStationItemDto>
{
    private readonly IAidStationsRepository _aidStationsRepository;
    private readonly IAidStationItemsRepository _aidStationItemsRepository;
    private readonly INutritionItemsRepository _nutritionItemsRepository;

    public CreateAidStationItemCommandHandler(
        IAidStationsRepository aidStationsRepository,
        IAidStationItemsRepository aidStationItemsRepository,
        INutritionItemsRepository nutritionItemsRepository)
    {
        _aidStationsRepository = aidStationsRepository;
        _aidStationItemsRepository = aidStationItemsRepository;
        _nutritionItemsRepository = nutritionItemsRepository;
    }

    public async Task<AidStationItemDto> Handle(CreateAidStationItemCommand command, CancellationToken cancellationToken)
    {
        _ = await _aidStationsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.AidStationId, cancellationToken)
            ?? throw new NotFoundException(nameof(AidStations), command.AidStationId);

        if (command.Request.NutritionItemId is not { } nutritionItemId)
        {
            throw new ValidationFailedException(
                "The item request is invalid.",
                [new ValidationFailure("InvalidNutritionItemId", "A nutrition item must be selected.")]);
        }

        var nutritionItem = await _nutritionItemsRepository.GetByIdAsync(
                command.AppUserId, nutritionItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(NutritionItems), nutritionItemId);

        var existing = await _aidStationItemsRepository.GetByAidStationIdAsync(
            command.AppUserId, command.RaceId, command.AidStationId, cancellationToken);
        AccountLimitRules.RequireRoom(existing.Count, AccountLimitRules.MaxItemsPerAidStation, "items at an aid station");

        var item = new AidStationItems
        {
            AidStationId = command.AidStationId,
            NutritionItemId = nutritionItemId,
            Position = existing.Count == 0 ? 1 : existing.Max(i => i.Position) + 1
        };

        await _aidStationItemsRepository.AddAsync(item, cancellationToken);
        await _aidStationItemsRepository.SaveChangesAsync(cancellationToken);

        return AidStationItemDto.FromEntity(item, nutritionItem);
    }
}
