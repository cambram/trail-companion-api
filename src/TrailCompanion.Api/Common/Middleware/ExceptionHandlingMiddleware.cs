using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TrailCompanion.Api.Common.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly ICorrelationContext _correlation;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        ILogger<ExceptionHandlingMiddleware> logger,
        ICorrelationContext correlation)
    {
        _next = next;
        _environment = environment;
        _logger = logger;
        _correlation = correlation;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            ValidationFailedException => (StatusCodes.Status400BadRequest, "Invalid request"),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogInformation("Request failed with {StatusCode}: {Message}",
                statusCode, exception.Message);
        }

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response had already started - could not write an error payload.");
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception is DomainException || _environment.IsDevelopment()
                ? exception.Message
                : null,
            Instance = context.Request.Path
        };

        problem.Extensions["correlationId"] = _correlation.CorrelationId;

        if (exception is ConflictException conflict)
        {
            problem.Extensions["validationErrors"] = new[] { new { code = conflict.Code, message = conflict.Message } };
        }

        if (exception is ValidationFailedException validationFailed)
        {
            problem.Extensions["validationErrors"] = validationFailed.Failures;
        }

        if (_environment.IsDevelopment() && exception is not DomainException)
        {
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, JsonOptions),
            context.RequestAborted);
    }
}
