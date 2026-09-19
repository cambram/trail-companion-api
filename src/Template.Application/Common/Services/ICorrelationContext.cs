namespace Template.Application.Common.Services;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}
