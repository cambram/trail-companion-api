namespace TrailCompanion.Application.Common.Services;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}
