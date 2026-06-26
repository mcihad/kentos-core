namespace Kentos.Infrastructure.Errors;

/// <summary>
/// A captured error. Repeats of the same fingerprint fold into a single row whose
/// <see cref="OccurrenceCount"/> / <see cref="LastSeenAt"/> advance. English type,
/// mapped to the Turkish 'hata_kayitlari' table. Excluded from auditing.
/// </summary>
public class ErrorLog
{
    public long Id { get; set; }

    /// <summary>Error fingerprint (from exception type + normalized message + top frame).</summary>
    public string Fingerprint { get; set; } = "";

    public ErrorOrigin Origin { get; set; } = ErrorOrigin.Server;

    /// <summary>Module slug where the error occurred.</summary>
    public string? Module { get; set; }

    public string Message { get; set; } = "";
    public string ExceptionType { get; set; } = "";
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? FileName { get; set; }
    public int? LineNumber { get; set; }
    public string? HttpMethod { get; set; }
    public string? Path { get; set; }
    public string? QueryString { get; set; }
    public int StatusCode { get; set; } = 500;
    public string? IpAddress { get; set; }

    /// <summary>Requesting client/browser identity (User-Agent).</summary>
    public string? UserAgent { get; set; }

    /// <summary>Request headers (jsonb).</summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    public string? UserId { get; set; }
    public string? UserName { get; set; }

    public ErrorStatus Status { get; set; } = ErrorStatus.New;
    public string? DeveloperNotes { get; set; }

    public int OccurrenceCount { get; set; } = 1;
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
