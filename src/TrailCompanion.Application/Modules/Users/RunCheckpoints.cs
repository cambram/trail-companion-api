using TrailCompanion.Domain.Modules.Users.Entities;

namespace TrailCompanion.Application.Modules.Users;

internal sealed record Checkpoint(string Key, Guid? AidStationId, string Name, int Index);

internal static class RunCheckpoints
{
    public const string FinishKey = "finish";

    public static IReadOnlyList<Checkpoint> Build(IReadOnlyList<Segments> segments, IReadOnlyList<AidStations> stations)
    {
        var positionBySegment = segments.ToDictionary(s => s.Id, s => s.Position);
        var checkpoints = new List<Checkpoint>();

        foreach (var station in stations
                     .Where(a => positionBySegment.ContainsKey(a.SegmentId))
                     .OrderBy(a => positionBySegment[a.SegmentId]))
        {
            checkpoints.Add(new Checkpoint(station.Id.ToString(), station.Id, station.Name, checkpoints.Count));
        }

        checkpoints.Add(new Checkpoint(FinishKey, null, "Finish", checkpoints.Count));

        return checkpoints;
    }

    public static bool TryParseKey(string? key, out Guid? aidStationId)
    {
        var trimmed = key?.Trim();

        if (string.Equals(trimmed, FinishKey, StringComparison.OrdinalIgnoreCase))
        {
            aidStationId = null;
            return true;
        }

        if (Guid.TryParse(trimmed, out var id))
        {
            aidStationId = id;
            return true;
        }

        aidStationId = null;
        return false;
    }

    public static Checkpoint? Find(IReadOnlyList<Checkpoint> checkpoints, string? key) =>
        TryParseKey(key, out var id)
            ? checkpoints.FirstOrDefault(c => c.AidStationId == id)
            : null;

    public static string KeyOf(Arrivals arrival) => arrival.AidStationId?.ToString() ?? FinishKey;
}
