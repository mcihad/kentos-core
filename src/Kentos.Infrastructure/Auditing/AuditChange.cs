namespace Kentos.Infrastructure.Auditing;

/// <summary>A single field-level audit change (stored inside jsonb).</summary>
public sealed record AuditChange(string Field, object? OldValue, object? NewValue);
