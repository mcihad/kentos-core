using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kentos.Infrastructure.Persistence;
using Kentos.SharedKernel.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kentos.Infrastructure.Errors;

/// <summary>EF-based error recorder; folds the same fingerprint into an occurrence count.</summary>
public sealed partial class ErrorRecorder : IErrorRecorder
{
    private static readonly string[] SensitiveHeaders = ["Authorization", "Cookie", "Set-Cookie"];

    private readonly AuditingDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly ILogger<ErrorRecorder> _logger;

    public ErrorRecorder(
        AuditingDbContext db,
        ICurrentUser currentUser,
        TimeProvider clock,
        ILogger<ErrorRecorder> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task RecordAsync(Exception exception, HttpContext httpContext, int statusCode, CancellationToken cancellationToken)
    {
        try
        {
            var fingerprint = ComputeFingerprint(exception);
            var now = _clock.GetUtcNow();

            var existing = await _db.ErrorLogs
                .FirstOrDefaultAsync(e => e.Fingerprint == fingerprint, cancellationToken);

            if (existing is not null)
            {
                existing.OccurrenceCount += 1;
                existing.LastSeenAt = now;
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }

            var (fileName, lineNumber, source) = ExtractFrame(exception);

            _db.ErrorLogs.Add(new ErrorLog
            {
                Fingerprint = fingerprint,
                Origin = ErrorOrigin.Server,
                Message = exception.Message,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                StackTrace = exception.StackTrace,
                Source = source,
                FileName = fileName,
                LineNumber = lineNumber,
                HttpMethod = httpContext.Request.Method,
                Path = httpContext.Request.Path.Value,
                QueryString = httpContext.Request.QueryString.HasValue ? httpContext.Request.QueryString.Value : null,
                StatusCode = statusCode,
                IpAddress = _currentUser.IpAddress,
                UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                Headers = CollectHeaders(httpContext),
                UserId = _currentUser.UserId,
                UserName = _currentUser.UserName,
                Status = ErrorStatus.New,
                OccurrenceCount = 1,
                FirstSeenAt = now,
                LastSeenAt = now,
            });

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception recordingError)
        {
            // Recording an error must never break the request flow.
            _logger.LogError(recordingError, "Failed to persist the error log.");
        }
    }

    private static string ComputeFingerprint(Exception exception)
    {
        var normalized = DigitsRegex().Replace(exception.Message, "#");
        normalized = GuidRegex().Replace(normalized, "#guid");
        var topFrame = exception.StackTrace?.Split('\n').FirstOrDefault()?.Trim() ?? "";
        var seed = $"{exception.GetType().FullName}|{normalized}|{topFrame}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexStringLower(hash);
    }

    private static (string? FileName, int? LineNumber, string? Source) ExtractFrame(Exception exception)
    {
        var trace = new StackTrace(exception, fNeedFileInfo: true);
        var frame = trace.GetFrame(0);
        if (frame is null)
        {
            return (null, null, null);
        }

        var method = frame.GetMethod();
        var source = method is null ? null : $"{method.DeclaringType?.FullName}.{method.Name}";
        var line = frame.GetFileLineNumber();
        return (frame.GetFileName(), line == 0 ? null : line, source);
    }

    private static Dictionary<string, string> CollectHeaders(HttpContext httpContext)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in httpContext.Request.Headers)
        {
            if (SensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            headers[header.Key] = header.Value.ToString();
        }

        return headers;
    }

    [GeneratedRegex("[0-9]+")]
    private static partial Regex DigitsRegex();

    [GeneratedRegex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F-]{27,}\b")]
    private static partial Regex GuidRegex();
}
