using System.Reflection;
using JasperFx.CodeGeneration.Model;
using Wolverine;
using Wolverine.FluentValidation;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>Wolverine (CQRS mediator + messaging) configuration.</summary>
public static class MessagingExtensions
{
    /// <summary>Configures Wolverine: FluentValidation middleware + module handler discovery.</summary>
    public static void Configure(WolverineOptions options, IEnumerable<Assembly> moduleAssemblies)
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
    }
}
