namespace Kentos.Infrastructure.Errors;

/// <summary>Triage status of an error record (ErrorLog.Status).</summary>
public enum ErrorStatus
{
    New,
    Investigating,
    Resolved,
    Ignored,
}
