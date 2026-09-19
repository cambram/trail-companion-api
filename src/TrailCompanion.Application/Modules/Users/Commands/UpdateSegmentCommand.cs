using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class UpdateSegmentCommand : IRequest<SegmentDto>
{
    public UpdateSegmentCommand(Guid appUserId, Guid raceId, Guid segmentId, SegmentRequest request)
    {
        AppUserId = appUserId;
        RaceId = raceId;
        SegmentId = segmentId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public Guid RaceId { get; }
    public Guid SegmentId { get; }
    public SegmentRequest Request { get; }
}

internal sealed class UpdateSegmentCommandHandler : IRequestHandler<UpdateSegmentCommand, SegmentDto>
{
    private readonly ISegmentsRepository _segmentsRepository;

    public UpdateSegmentCommandHandler(ISegmentsRepository segmentsRepository)
    {
        _segmentsRepository = segmentsRepository;
    }

    public async Task<SegmentDto> Handle(UpdateSegmentCommand command, CancellationToken cancellationToken)
    {
        var segment = await _segmentsRepository.GetByIdAsync(
                command.AppUserId, command.RaceId, command.SegmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Segments), command.SegmentId);

        SegmentRequestRules.RequireValid(command.Request);

        segment.Name = command.Request.Name!.Trim();
        segment.Duration = command.Request.Duration ?? 0;
        segment.Distance = command.Request.Distance;
        segment.Notes = command.Request.Notes ?? string.Empty;

        await _segmentsRepository.SaveChangesAsync(cancellationToken);

        return SegmentDto.FromEntity(segment);
    }
}
