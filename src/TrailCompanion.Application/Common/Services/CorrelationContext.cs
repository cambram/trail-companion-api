namespace TrailCompanion.Application.Common.Services;

public sealed class CorrelationContext : ICorrelationContext
{
    public const string Unassigned = "unassigned";

    public const int MaxLength = 64;

    private static readonly AsyncLocal<Holder?> Ambient = new();

    public string CorrelationId => Ambient.Value?.Value ?? Unassigned;

    public IDisposable Begin(string correlationId)
    {
        if (!IsAcceptable(correlationId))
        {
            throw new ArgumentException(
                $"'{correlationId}' is not a usable correlation id. Pass {nameof(Sanitize)}'d input.",
                nameof(correlationId));
        }

        var previous = Ambient.Value;
        Ambient.Value = new Holder { Value = correlationId };

        return new Restorer(previous);
    }

    public static string NewId() => Guid.NewGuid().ToString("N");

    public static string Sanitize(string? candidate) => IsAcceptable(candidate) ? candidate! : NewId();

    public static bool IsAcceptable(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || candidate.Length > MaxLength)
        {
            return false;
        }

        if (string.Equals(candidate, Unassigned, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (var character in candidate)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.'))
            {
                return false;
            }
        }

        return true;
    }

    private sealed class Holder
    {
        public string? Value;
    }

    private sealed class Restorer : IDisposable
    {
        private readonly Holder? _previous;

        private bool _disposed;

        public Restorer(Holder? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            var current = Ambient.Value;
            if (current is not null)
            {
                current.Value = null;
            }

            Ambient.Value = _previous;
        }
    }
}
