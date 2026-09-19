namespace TrailCompanion.Api.Common.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly CorrelationContext _correlation;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        CorrelationContext correlation,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _correlation = correlation;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationContext.Sanitize(context.Request.Headers[HeaderName]);

        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(static state =>
        {
            var (response, id) = ((HttpResponse, string))state;
            response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        }, (context.Response, correlationId));

        using var correlationScope = _correlation.Begin(correlationId);
        using var loggerScope = _logger.BeginScope("CorrelationId:{CorrelationId}", correlationId);

        await _next(context);
    }
}
