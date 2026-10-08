namespace TrailCompanion.Infrastructure.IntegrationTests;

public static class TestPins
{
    private static int _next;

    public static string Next() => Interlocked.Increment(ref _next).ToString("D6");
}
