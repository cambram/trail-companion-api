using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class UpdateAidStationCommand : IRequest<AidStationDto>
{
    public UpdateAidStationCommand(Guid appUserId, Guid raceId, Guid aidStationId, AidStationRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        AidStationId = aidStationId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid AidStationId { get; }
    public AidStationRequest Request { get; }
}

internal sealed class UpdateAidStationCommandHandler : IRequestHandler<UpdateAidStationCommand, AidStationDto>
{
    private readonly IAidStationsRepository _aidStationsRepository;

    public UpdateAidStationCommandHandler(IAidStationsRepository aidStationsRepository)
    {
        _aidStationsRepository = aidStationsRepository;
    }

    public async Task<AidStationDto> Handle(UpdateAidStationCommand command, CancellationToken cancellationToken)
    {
        var station = await _aidStationsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.AidStationId, cancellationToken)
            ?? throw new NotFoundException(nameof(AidStations), command.AidStationId);

        AidStationRequestRules.RequireValid(command.Request);

        station.Name = command.Request.Name!.Trim();
        station.Notes = command.Request.Notes ?? string.Empty;
        station.Kit = (command.Request.Kit ?? [])
            .Select(e => new KitEntry { Label = e.Label!.Trim(), Certainty = e.Certainty!.Value })
            .ToList();

        await _aidStationsRepository.SaveChangesAsync(cancellationToken);

        return AidStationDto.FromEntity(station);
    }
}
