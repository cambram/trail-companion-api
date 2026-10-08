using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class ReorderSegmentsCommand : IRequest
{
    public ReorderSegmentsCommand(Guid appUserId, Guid raceId, ReorderRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public ReorderRequest Request { get; }
}

internal sealed class ReorderSegmentsCommandHandler : IRequestHandler<ReorderSegmentsCommand>
{
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly IRaceRunsRepository _runsRepository;

    public ReorderSegmentsCommandHandler(ISegmentsRepository segmentsRepository, IRaceRunsRepository runsRepository)
    {
        _segmentsRepository = segmentsRepository;
        _runsRepository = runsRepository;
    }

    public async Task Handle(ReorderSegmentsCommand command, CancellationToken cancellationToken)
    {
        var ids = ReorderRequestRules.RequireIds(command.Request);
        var segments = new List<Segments>();

        foreach (var id in ids)
        {
            segments.Add(await _segmentsRepository.GetByIdAsync(command.AppUserId, command.RaceId, id, cancellationToken)
                ?? throw new NotFoundException(nameof(Segments), id));
        }

        var existing = await _segmentsRepository.GetByRaceIdAsync(command.AppUserId, command.RaceId, cancellationToken);
        ReorderRequestRules.RequireComplete(segments.Count, existing.Count);

        await RunGuard.RequireNoActiveRunAsync(_runsRepository, command.RaceId, cancellationToken);

        for (var i = 0; i < segments.Count; i++)
        {
            segments[i].Position = i + 1;
        }

        await _segmentsRepository.SaveChangesAsync(cancellationToken);
    }
}
