using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class CreateAidStationCommand : IRequest<AidStationDto>
{
    public CreateAidStationCommand(Guid appUserId, Guid raceId, CreateAidStationRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public CreateAidStationRequest Request { get; }
}

internal sealed class CreateAidStationCommandHandler : IRequestHandler<CreateAidStationCommand, AidStationDto>
{
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly IAidStationsRepository _aidStationsRepository;
    private readonly IRaceRunsRepository _runsRepository;

    public CreateAidStationCommandHandler(
        ISegmentsRepository segmentsRepository,
        IAidStationsRepository aidStationsRepository,
        IRaceRunsRepository runsRepository)
    {
        _segmentsRepository = segmentsRepository;
        _aidStationsRepository = aidStationsRepository;
        _runsRepository = runsRepository;
    }

    public async Task<AidStationDto> Handle(CreateAidStationCommand command, CancellationToken cancellationToken)
    {
        if (command.Request.SegmentId is not { } segmentId)
        {
            throw new ValidationFailedException(
                "The aid station request is invalid.",
                [new ValidationFailure("InvalidSegmentId", "A segment must be selected.")]);
        }

        AidStationRequestRules.RequireValid(new AidStationRequest(command.Request.Name, null, null));

        _ = await _segmentsRepository.GetByIdAsync(command.AppUserId, command.RaceId, segmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Segments), segmentId);

        await RunGuard.RequireNoActiveRunAsync(_runsRepository, command.RaceId, cancellationToken);

        if (await _aidStationsRepository.GetBySegmentIdAsync(
                command.AppUserId, command.RaceId, segmentId, cancellationToken) is not null)
        {
            throw new ConflictException("SegmentHasAidStation", "This segment already has an aid station.");
        }

        var station = new AidStations { SegmentId = segmentId, Name = command.Request.Name!.Trim() };

        await _aidStationsRepository.AddAsync(station, cancellationToken);
        await _aidStationsRepository.SaveChangesAsync(cancellationToken);

        return AidStationDto.FromEntity(station);
    }
}
