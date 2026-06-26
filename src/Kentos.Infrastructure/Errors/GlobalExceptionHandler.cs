using System.Diagnostics;
using FluentValidation;
using Kentos.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kentos.Infrastructure.Errors;

/// <summary>
/// Catches every exception, converts it to an RFC7807 ProblemDetails, logs it, and
/// persists 5xx errors as <see cref="ErrorLog"/>.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, errorCode, title) = Map(exception);
        var errors = exception is ValidationException validation ? ToErrors(validation) : null;

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled error: {Message}", exception.Message);
            var recorder = httpContext.RequestServices.GetRequiredService<IErrorRecorder>();
            await recorder.RecordAsync(exception, httpContext, status, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Handled error {ErrorCode} ({Status}): {Message}", errorCode, status, exception.Message);
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://kentos.local/errors/{errorCode}",
            Detail = _environment.IsDevelopment()
                ? exception.ToString()
                : status >= 500 ? "Beklenmeyen bir hata oluştu." : exception.Message,
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = traceId;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string ErrorCode, string Title) Map(Exception exception) => exception switch
    {
        ValidationException => (400, "validation", "Doğrulama hatası"),
        KentosException kentos => (kentos.StatusCode, kentos.ErrorCode, kentos.Message),
        DbUpdateConcurrencyException => (409, "concurrency", "Eşzamanlılık çakışması"),
        _ => (500, "internal", "Sunucu hatası"),
    };

    private static Dictionary<string, string[]> ToErrors(ValidationException exception) =>
        exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
