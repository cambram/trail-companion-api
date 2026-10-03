using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteAidStationCommand : IRequest
{
    public DeleteAidStationCommand(Guid appUserId, Guid raceId, Guid aidStationId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        AidStationId = aidStationId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid AidStationId { get; }
}

internal sealed class DeleteAidStationCommandHandler : IRequestHandler<DeleteAidStationCommand>
{
    private readonly IAidStationsRepository _aidStationsRepository;

    public DeleteAidStationCommandHandler(IAidStationsRepository aidStationsRepository)
    {
        _aidStationsRepository = aidStationsRepository;
    }

    public async Task Handle(DeleteAidStationCommand command, CancellationToken cancellationToken)
    {
        var station = await _aidStationsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.AidStationId, cancellationToken)
            ?? throw new NotFoundException(nameof(AidStations), command.AidStationId);

        _aidStationsRepository.Delete(station);
        await _aidStationsRepository.SaveChangesAsync(cancellationToken);
    }
}
