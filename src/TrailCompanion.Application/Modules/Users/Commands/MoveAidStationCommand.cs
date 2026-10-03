using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class MoveAidStationCommand : IRequest
{
    public MoveAidStationCommand(Guid appUserId, Guid raceId, Guid aidStationId, MoveAidStationRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        AidStationId = aidStationId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid AidStationId { get; }
    public MoveAidStationRequest Request { get; }
}

internal sealed class MoveAidStationCommandHandler : IRequestHandler<MoveAidStationCommand>
{
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly IAidStationsRepository _aidStationsRepository;

    public MoveAidStationCommandHandler(
        ISegmentsRepository segmentsRepository,
        IAidStationsRepository aidStationsRepository)
    {
        _segmentsRepository = segmentsRepository;
        _aidStationsRepository = aidStationsRepository;
    }

    public async Task Handle(MoveAidStationCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.SegmentId is not { } targetId)
        {
            throw new ValidationFailedException(
                "The move request is invalid.",
                [new ValidationFailure("InvalidSegmentId", "A target segment must be selected.")]);
        }

        var station = await _aidStationsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.AidStationId, cancellationToken)
            ?? throw new NotFoundException(nameof(AidStations), command.AidStationId);

        _ = await _segmentsRepository.GetByIdAsync(command.AppUserId, command.RaceId, targetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Segments), targetId);

        if (station.SegmentId == targetId)
        {
            return;
        }

        var occupant = await _aidStationsRepository.GetBySegmentIdAsync(
            command.AppUserId, command.RaceId, targetId, cancellationToken);

        if (occupant is not null)
        {
            occupant.SegmentId = station.SegmentId;
        }

        station.SegmentId = targetId;

        await _aidStationsRepository.SaveChangesAsync(cancellationToken);
    }
}
