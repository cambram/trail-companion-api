using System.Collections.Frozen;

namespace TrailCompanion.Infrastructure.Common.Persistence;

internal static class UniqueConstraintConflicts
{
    private static readonly FrozenDictionary<string, (string Code, string Message)> ByConstraintName =
        new Dictionary<string, (string Code, string Message)>
        {
            ["IX_AppUsers_Username"] =
                ("DuplicateUsername", "That username is taken."),
            ["IX_NutritionItems_AppUserId_Name"] =
                ("DuplicateNutritionItem", "You already have a nutrition item with this name."),
            ["UQ_AidStations_SegmentId"] =
                ("SegmentHasAidStation", "This segment already has an aid station."),
            ["IX_RaceCrewMembers_RaceId_AppUserId"] =
                ("AlreadyCrew", "You are already on this race's crew."),
            ["IX_Races_Pin"] =
                ("PinTaken", "Could not assign a race PIN. Try again."),
            ["IX_RaceRuns_RaceId"] =
                ("RunExists", "This race has already been started."),
            ["IX_Arrivals_RaceRunId_AidStationId"] =
                ("ArrivalExists", "That checkpoint has already been recorded."),
        }.ToFrozenDictionary();

    public static bool TryTranslate(string? constraintName, out (string Code, string Message) conflict)
    {
        if (constraintName is null)
        {
            conflict = default;
            return false;
        }

        return ByConstraintName.TryGetValue(constraintName, out conflict);
    }
}
