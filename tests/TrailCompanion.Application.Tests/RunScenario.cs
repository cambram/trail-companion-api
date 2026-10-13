using TrailCompanion.Application.Common.Services;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

internal sealed class RunScenario
{
    public static readonly DateOnly RaceDate = new(2026, 10, 11);
    public static readonly DateTimeOffset RaceMorning = new(2026, 10, 11, 4, 0, 0, TimeSpan.Zero);

    public RunScenario(DateOnly? raceDate = null)
    {
        Race = new Races
        {
            Id = Guid.NewGuid(),
            AppUserId = OwnerId,
            Name = "UTCT",
            RaceDate = raceDate ?? RaceDate,
            Pin = "482913"
        };

        int[] durations = [180, 120, 180, 180];
        SegmentRows = durations
            .Select((minutes, i) => new Segments
            {
                Id = Guid.NewGuid(),
                RaceId = Race.Id,
                Position = i + 1,
                Name = $"S{i + 1}",
                Duration = minutes
            })
            .ToList();

        StationRows = Enumerable.Range(1, 3)
            .Select(i => new AidStations { Id = Guid.NewGuid(), SegmentId = SegmentRows[i].Id, Name = $"AS{i}" })
            .ToList();

        RacesRepo.GetAccessibleByIdAsync(OwnerId, Race.Id, Arg.Any<CancellationToken>()).Returns(Race);
        RacesRepo.GetAccessibleByIdAsync(CrewId, Race.Id, Arg.Any<CancellationToken>()).Returns(Race);
        RacesRepo.GetAccessibleByIdAsync(StrangerId, Race.Id, Arg.Any<CancellationToken>()).Returns((Races?)null);
        SegmentsRepo.GetByRaceIdAsync(OwnerId, Race.Id, Arg.Any<CancellationToken>()).Returns(SegmentRows);
        StationsRepo.GetByRaceIdAsync(OwnerId, Race.Id, Arg.Any<CancellationToken>()).Returns(StationRows);
        RunsRepo.GetByRaceIdAsync(Arg.Any<Guid>(), Race.Id, Arg.Any<CancellationToken>()).Returns((RaceRuns?)null);
        RunsRepo.GetArrivalsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ => (IReadOnlyList<Arrivals>)ArrivalRows.ToList());
    }

    public IRacesRepository RacesRepo { get; } = Substitute.For<IRacesRepository>();
    public IRaceRunsRepository RunsRepo { get; } = Substitute.For<IRaceRunsRepository>();
    public ISegmentsRepository SegmentsRepo { get; } = Substitute.For<ISegmentsRepository>();
    public IAidStationsRepository StationsRepo { get; } = Substitute.For<IAidStationsRepository>();
    public ManualClock Clock { get; } = new(RaceMorning);
    public AppTimeZone Zone { get; } = AppTimeZone.Resolve("Africa/Johannesburg");
    public Guid OwnerId { get; } = Guid.NewGuid();
    public Guid CrewId { get; } = Guid.NewGuid();
    public Guid StrangerId { get; } = Guid.NewGuid();
    public Races Race { get; }
    public List<Segments> SegmentRows { get; }
    public List<AidStations> StationRows { get; }
    public List<Arrivals> ArrivalRows { get; } = [];
    public RaceRuns? Run { get; private set; }

    public Guid RaceId => Race.Id;
    public AidStations As1 => StationRows[0];
    public AidStations As2 => StationRows[1];
    public AidStations As3 => StationRows[2];

    public DateTime Utc(double minutesAfterStart) => RaceMorning.UtcDateTime.AddMinutes(minutesAfterStart);

    public RaceRuns GivenRun(double startMinutesAfterMorning = 0)
    {
        Run = new RaceRuns
        {
            Id = Guid.NewGuid(),
            RaceId = Race.Id,
            StartedAtUtc = Utc(startMinutesAfterMorning),
            StartedBy = CrewId
        };
        RunsRepo.GetByRaceIdAsync(OwnerId, Race.Id, Arg.Any<CancellationToken>()).Returns(Run);
        RunsRepo.GetByRaceIdAsync(CrewId, Race.Id, Arg.Any<CancellationToken>()).Returns(Run);
        RunsRepo.GetByRaceIdAsync(StrangerId, Race.Id, Arg.Any<CancellationToken>()).Returns((RaceRuns?)null);
        return Run;
    }

    public Arrivals GivenArrival(AidStations? station, double minutesAfterStart)
    {
        var arrival = new Arrivals
        {
            Id = Guid.NewGuid(),
            RaceRunId = Run!.Id,
            AidStationId = station?.Id,
            ArrivedAtUtc = Utc(minutesAfterStart),
            RecordedBy = CrewId
        };
        ArrivalRows.Add(arrival);
        if (station is null)
        {
            Run.FinishedAtUtc = arrival.ArrivedAtUtc;
        }

        return arrival;
    }
}
