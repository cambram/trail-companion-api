using System.Globalization;
using TrailCompanion.Application.Common.Services;

namespace TrailCompanion.Application.Tests;

public class AppTimeZoneTests
{
    private static AppTimeZone Johannesburg() => AppTimeZone.Resolve("Africa/Johannesburg");

    [Fact]
    public void Resolve_KnownZone_ReturnsIt()
    {
        Assert.Equal("Africa/Johannesburg", Johannesburg().Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Nowhere/Land")]
    public void Resolve_MissingOrUnknownZone_ThrowsNamingTheKey(string? id)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => AppTimeZone.Resolve(id));

        Assert.Contains("App:TimeZone", exception.Message);
    }

    [Theory]
    [InlineData("2026-10-10T21:59:59Z", "2026-10-10")]
    [InlineData("2026-10-10T22:00:00Z", "2026-10-11")]
    [InlineData("2026-10-11T21:59:59Z", "2026-10-11")]
    [InlineData("2026-10-11T22:00:00Z", "2026-10-12")]
    public void DateOf_FollowsTheJohannesburgWallClock(string utc, string expected)
    {
        var date = Johannesburg().DateOf(DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture));

        Assert.Equal(DateOnly.Parse(expected, CultureInfo.InvariantCulture), date);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Clock_ShowsLocalHoursAndMinutes(DateTimeKind kind)
    {
        var instant = new DateTime(2026, 10, 11, 4, 5, 59, kind);

        Assert.Equal("06:05", Johannesburg().Clock(instant));
    }
}
