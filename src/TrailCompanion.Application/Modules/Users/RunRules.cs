using System.Globalization;
using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users;

internal static class RunRules
{
    public static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    public static DateTime ToUtc(DateTimeOffset instant)
    {
        var utc = instant.UtcDateTime;

        return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    }

    public static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static ValidationFailedException Invalid(string code, string message) =>
        new("The live tracking request is invalid.", [new ValidationFailure(code, message)]);

    public static void RequireStart(
        AppTimeZone zone,
        Races race,
        DateTime startedAtUtc,
        DateTimeOffset now,
        DateTime? firstArrivalUtc)
    {
        if (startedAtUtc > now.UtcDateTime + ClockSkew)
        {
            throw Invalid("StartInFuture", "The start time cannot be in the future.");
        }

        if (race.RaceDate is not { } raceDate)
        {
            throw Invalid("RaceDateNotSet", "Set the race date before changing the start time.");
        }

        if (zone.DateOf(startedAtUtc) != raceDate)
        {
            throw Invalid("StartNotOnRaceDay", $"The start time must be on race day ({Date(raceDate)}).");
        }

        if (firstArrivalUtc is { } first && startedAtUtc >= first)
        {
            throw Invalid(
                "StartAfterFirstArrival",
                $"The start must be before the first recorded arrival ({zone.Clock(first)}).");
        }
    }

    public static void RequireArrival(
        AppTimeZone zone,
        DateTime startedAtUtc,
        IReadOnlyList<Checkpoint> checkpoints,
        IReadOnlyDictionary<int, DateTime> recordedByIndex,
        Checkpoint target,
        DateTime arrivedAtUtc,
        DateTimeOffset now)
    {
        if (arrivedAtUtc > now.UtcDateTime + ClockSkew)
        {
            throw Invalid("ArrivalInFuture", "An arrival time cannot be in the future.");
        }

        if (arrivedAtUtc <= startedAtUtc)
        {
            throw Invalid(
                "ArrivalBeforeStart",
                $"{target.Name} must be after the start ({zone.Clock(startedAtUtc)}).");
        }

        var before = recordedByIndex
            .Where(p => p.Key < target.Index)
            .OrderByDescending(p => p.Key)
            .Select(p => (Index: p.Key, At: p.Value))
            .FirstOrDefault();

        if (before != default && arrivedAtUtc <= before.At)
        {
            throw Invalid(
                "ArrivalOutOfOrder",
                $"{target.Name} must be after {checkpoints[before.Index].Name} ({zone.Clock(before.At)}).");
        }

        var after = recordedByIndex
            .Where(p => p.Key > target.Index)
            .OrderBy(p => p.Key)
            .Select(p => (Index: p.Key, At: p.Value))
            .FirstOrDefault();

        if (after != default && arrivedAtUtc >= after.At)
        {
            throw Invalid(
                "ArrivalOutOfOrder",
                $"{target.Name} must be before {checkpoints[after.Index].Name} ({zone.Clock(after.At)}).");
        }
    }
}
