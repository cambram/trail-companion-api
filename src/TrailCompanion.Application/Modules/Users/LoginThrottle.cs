using System.Collections.Concurrent;

namespace TrailCompanion.Application.Modules.Users;

// ponytail: in-memory and per instance, cleared on restart; move to a table if the API runs more than one instance
internal sealed class LoginThrottle
{
    internal const int MaxFailures = 10;
    internal static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private const int PruneThreshold = 10_000;

    private readonly ConcurrentDictionary<string, Failures> _failures = new();
    private readonly TimeProvider _time;

    public LoginThrottle(TimeProvider time)
    {
        _time = time;
    }

    public bool IsBlocked(string username) =>
        _failures.TryGetValue(username, out var failures)
        && failures.Count >= MaxFailures
        && _time.GetUtcNow() - failures.FirstAtUtc < Window;

    public void RecordFailure(string username)
    {
        var now = _time.GetUtcNow();

        _failures.AddOrUpdate(
            username,
            _ => new Failures(1, now),
            (_, existing) => now - existing.FirstAtUtc >= Window
                ? new Failures(1, now)
                : existing with { Count = existing.Count + 1 });

        if (_failures.Count > PruneThreshold)
        {
            foreach (var (key, failures) in _failures)
            {
                if (now - failures.FirstAtUtc >= Window)
                {
                    _failures.TryRemove(key, out _);
                }
            }
        }
    }

    public void Reset(string username) => _failures.TryRemove(username, out _);

    private sealed record Failures(int Count, DateTimeOffset FirstAtUtc);
}
