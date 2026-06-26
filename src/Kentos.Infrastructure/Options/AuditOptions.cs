namespace Kentos.Infrastructure.Options;

/// <summary>Audit log provider.</summary>
public enum AuditProvider
{
    /// <summary>Development: writes to the Postgres 'denetim' schema.</summary>
    Postgres,

    /// <summary>Production: writes to MongoDB.</summary>
    Mongo,
}

/// <summary>Audit settings (the "Audit" section).</summary>
public sealed class AuditOptions
{
    public const string SectionName = "Audit";

    public AuditProvider Provider { get; set; } = AuditProvider.Postgres;
}
