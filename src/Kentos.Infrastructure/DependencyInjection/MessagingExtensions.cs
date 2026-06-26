using System.Reflection;
using JasperFx.CodeGeneration.Model;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Postgresql;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>Wolverine (CQRS mediator + messaging) configuration.</summary>
public static class MessagingExtensions
{
    /// <summary>Wolverine internal envelope storage lives in this Postgres schema.</summary>
    public const string MessagingSchema = "mesajlasma";

    /// <summary>
    /// Configures Wolverine: FluentValidation middleware, module handler discovery, and —
    /// when a Postgres connection string is supplied — a durable transactional outbox so
    /// published domain events are persisted and delivered reliably (retries, dead-letter,
    /// survive restarts) instead of in-memory only.
    /// </summary>
    public static void Configure(
        WolverineOptions options, string? postgresConnectionString, IEnumerable<Assembly> moduleAssemblies)
    {
        // Handlers receive EF Core DbContext + Mapster IMapper via DI; both require
        // service location, which Wolverine 6 disallows by default.
        options.ServiceLocationPolicy = ServiceLocationPolicy.AlwaysAllowed;

        // Validators are registered explicitly per module (AddValidatorsFromAssembly);
        // tell Wolverine to use those rather than discovering + registering its own
        // (which would run each validator twice → duplicate error messages).
        options.UseFluentValidation(RegistrationBehavior.ExplicitRegistration);

        foreach (var assembly in moduleAssemblies.Distinct())
        {
            options.Discovery.IncludeAssembly(assembly);
        }

        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            // Durable message store (envelopes persisted in the "mesajlasma" schema).
            options.PersistMessagesWithPostgresql(postgresConnectionString, MessagingSchema);

            // Wrap each handler in a transaction and route its outgoing messages through
            // the outbox (store-then-forward) for at-least-once delivery.
            options.Policies.AutoApplyTransactions();
        }
    }
}
