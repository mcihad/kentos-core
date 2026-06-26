using Kentos.Infrastructure.Auditing;
using Kentos.Infrastructure.Errors;
using Kentos.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Infrastructure.Persistence;

/// <summary>
/// The 'denetim' schema: audit (AuditLog) and error (ErrorLog) tables. A plain
/// DbContext — these infrastructure tables are not subject to audit/soft-delete.
/// </summary>
public sealed class AuditingDbContext : DbContext
{
    public const string Schema = "denetim";

    public AuditingDbContext(DbContextOptions<AuditingDbContext> options) : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new ErrorLogConfiguration());
    }
}
