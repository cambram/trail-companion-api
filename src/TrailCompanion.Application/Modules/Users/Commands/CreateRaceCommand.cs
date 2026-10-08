using TrailCompanion.Application.Modules.Users;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using MediatR;

namespace TrailCompanion.Application.Modules.Users.Commands;

public sealed class CreateRaceCommand : IRequest<RaceDto>
{
    public const int MaxNameLength = 200;

    public CreateRaceCommand(Guid appUserId, CreateRaceRequest request)
    {
        AppUserId = appUserId;
        Request = request;
    }

    public Guid AppUserId { get; }
    public CreateRaceRequest Request { get; }
}

internal sealed class CreateRaceCommandHandler : IRequestHandler<CreateRaceCommand, RaceDto>
{
    private readonly IRacesRepository _racesRepository;

    public CreateRaceCommandHandler(IRacesRepository racesRepository)
    {
        _racesRepository = racesRepository;
    }

    public async Task<RaceDto> Handle(CreateRaceCommand command, CancellationToken cancellationToken)
    {
        var name = command.Request.Name?.Trim() ?? string.Empty;

        RequireValid(name, command.Request.Target);

        var existing = await _racesRepository.GetAllAsync(command.AppUserId, cancellationToken);
        AccountLimitRules.RequireRoom(existing.Count, AccountLimitRules.MaxRaces, "races");

        var race = new Races
        {
            AppUserId = command.AppUserId,
            Name = name,
            StartTime = command.Request.StartTime,
            RaceDate = command.Request.RaceDate,
            Pin = await RacePins.GenerateUniqueAsync(_racesRepository, cancellationToken)
        };

        if (command.Request.Target is { } target)
        {
            race.Target = target;
        }

        await _racesRepository.AddAsync(race, cancellationToken);
        await _racesRepository.SaveChangesAsync(cancellationToken);

        return RaceDto.FromEntity(race);
    }

    internal static void RequireValid(string name, int? target)
    {
        var failures = new List<ValidationFailure>();

        if (name.Length is 0 or > CreateRaceCommand.MaxNameLength)
        {
            failures.Add(new ValidationFailure(
                "InvalidName",
                $"Name is required and must be at most {CreateRaceCommand.MaxNameLength} characters."));
        }

        if (target < 0)
        {
            failures.Add(new ValidationFailure("InvalidTarget", "Target must not be negative."));
        }

        if (failures.Count > 0)
        {
            throw new ValidationFailedException("The race request is invalid.", failures);
        }
    }
}
