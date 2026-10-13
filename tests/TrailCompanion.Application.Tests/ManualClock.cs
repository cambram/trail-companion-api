namespace TrailCompanion.Application.Tests;

internal sealed class ManualClock : TimeProvider
{
    public ManualClock(DateTimeOffset now)
    {
        Now = now;
    }

    public DateTimeOffset Now { get; set; }

    public override DateTimeOffset GetUtcNow() => Now;
}
