using Kentos.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kentos.Infrastructure.Persistence;

/// <summary>Applies migrations for every registered DbContext (auditing + module contexts).</summary>
public static class MigrationRunner
{
    public static async Task MigrateAllAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var registrations = scope.ServiceProvider.GetServices<DbContextRegistration>();

        foreach (var registration in registrations)
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(registration.ContextType);
            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
