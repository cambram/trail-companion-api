using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class CreateSegmentCommand : IRequest<SegmentDto>
{
    public CreateSegmentCommand(Guid appUserId, Guid raceId, SegmentRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public SegmentRequest Request { get; }
}

internal sealed class CreateSegmentCommandHandler : IRequestHandler<CreateSegmentCommand, SegmentDto>
{
    private readonly IRacesRepository _racesRepository;
    private readonly ISegmentsRepository _segmentsRepository;

    public CreateSegmentCommandHandler(IRacesRepository racesRepository, ISegmentsRepository segmentsRepository)
    {
        _racesRepository = racesRepository;
        _segmentsRepository = segmentsRepository;
    }

    public async Task<SegmentDto> Handle(CreateSegmentCommand command, CancellationToken cancellationToken)
    {
        _ = await _racesRepository.GetByIdAsync(command.AppUserId, command.RaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Races), command.RaceId);

        SegmentRequestRules.RequireValid(command.Request);

        var existing = await _segmentsRepository.GetByRaceIdAsync(command.AppUserId, command.RaceId, cancellationToken);
        AccountLimitRules.RequireRoom(existing.Count, AccountLimitRules.MaxSegmentsPerRace, "segments in a race");

        var segment = new Segments
        {
            RaceId = command.RaceId,
            Position = existing.Count == 0 ? 1 : existing.Max(s => s.Position) + 1,
            Name = command.Request.Name!.Trim(),
            Duration = command.Request.Duration ?? 0,
            EndKm = command.Request.EndKm,
            AscentM = command.Request.AscentM,
            Notes = command.Request.Notes ?? string.Empty
        };

        await _segmentsRepository.AddAsync(segment, cancellationToken);
        await _segmentsRepository.SaveChangesAsync(cancellationToken);

        return SegmentDto.FromEntity(segment);
    }
}
