using System.Globalization;

namespace TrailCompanion.Application.Common.Services;

public sealed class AppTimeZone
{
    public const string ConfigurationKey = "App:TimeZone";

    public AppTimeZone(TimeZoneInfo zone)
    {
        Zone = zone;
    }

    public TimeZoneInfo Zone { get; }

    public string Id => Zone.Id;

    public DateOnly DateOf(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    public DateOnly DateOf(DateTime utc) => DateOf(AsUtc(utc));

    public string Clock(DateTime utc) =>
        TimeZoneInfo.ConvertTime(AsUtc(utc), Zone).ToString("HH:mm", CultureInfo.InvariantCulture);

    public static AppTimeZone Resolve(string? id)
    {
        var trimmed = id?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new InvalidOperationException(
                $"'{ConfigurationKey}' is not set. Set it to an IANA time zone id such as Africa/Johannesburg.");
        }

        try
        {
            return new AppTimeZone(TimeZoneInfo.FindSystemTimeZoneById(trimmed));
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"'{ConfigurationKey}' is '{trimmed}', which this machine cannot resolve. " +
                "Use an IANA time zone id and make sure the operating system has tzdata installed.",
                exception);
        }
    }

    private static DateTimeOffset AsUtc(DateTime utc) =>
        new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
