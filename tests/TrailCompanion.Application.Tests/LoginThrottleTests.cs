using TrailCompanion.Application.Modules.Users;

namespace TrailCompanion.Application.Tests;

public class LoginThrottleTests
{
    private readonly ManualTime _time = new();

    private LoginThrottle CreateThrottle() => new(_time);

    [Fact]
    public void IsBlocked_AfterMaxFailures_BlocksThatUsernameOnly()
    {
        var throttle = CreateThrottle();

        for (var i = 0; i < LoginThrottle.MaxFailures; i++)
        {
            throttle.RecordFailure("runner");
        }

        Assert.True(throttle.IsBlocked("runner"));
        Assert.False(throttle.IsBlocked("someone-else"));
    }

    [Fact]
    public void IsBlocked_OneFailureShortOfTheLimit_DoesNotBlock()
    {
        var throttle = CreateThrottle();

        for (var i = 0; i < LoginThrottle.MaxFailures - 1; i++)
        {
            throttle.RecordFailure("runner");
        }

        Assert.False(throttle.IsBlocked("runner"));
    }

    [Fact]
    public void IsBlocked_AfterTheWindowPasses_Unblocks()
    {
        var throttle = CreateThrottle();

        for (var i = 0; i < LoginThrottle.MaxFailures; i++)
        {
            throttle.RecordFailure("runner");
        }

        _time.Now += LoginThrottle.Window;

        Assert.False(throttle.IsBlocked("runner"));
    }

    [Fact]
    public void Reset_ClearsFailures()
    {
        var throttle = CreateThrottle();

        for (var i = 0; i < LoginThrottle.MaxFailures; i++)
        {
            throttle.RecordFailure("runner");
        }

        throttle.Reset("runner");

        Assert.False(throttle.IsBlocked("runner"));
    }

    private sealed class ManualTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
