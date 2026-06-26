using Microsoft.AspNetCore.Http;

namespace Kentos.Infrastructure.Errors;

/// <summary>Persists captured errors as <see cref="ErrorLog"/> (folding by fingerprint).</summary>
public interface IErrorRecorder
{
    Task RecordAsync(Exception exception, HttpContext httpContext, int statusCode, CancellationToken cancellationToken);
}
