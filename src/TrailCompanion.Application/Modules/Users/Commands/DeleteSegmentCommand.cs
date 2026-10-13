using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class DeleteSegmentCommand : IRequest
{
    public DeleteSegmentCommand(Guid appUserId, Guid raceId, Guid segmentId)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        SegmentId = segmentId;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid SegmentId { get; }
}

internal sealed class DeleteSegmentCommandHandler : IRequestHandler<DeleteSegmentCommand>
{
    private readonly ISegmentsRepository _segmentsRepository;
    private readonly IRaceRunsRepository _runsRepository;

    public DeleteSegmentCommandHandler(ISegmentsRepository segmentsRepository, IRaceRunsRepository runsRepository)
    {
        _segmentsRepository = segmentsRepository;
        _runsRepository = runsRepository;
    }

    public async Task Handle(DeleteSegmentCommand command, CancellationToken cancellationToken)
    {
        var segment = await _segmentsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.SegmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Segments), command.SegmentId);

        await RunGuard.RequireNoActiveRunAsync(_runsRepository, command.RaceId, cancellationToken);

        _segmentsRepository.Delete(segment);
        await _segmentsRepository.SaveChangesAsync(cancellationToken);
    }
}
