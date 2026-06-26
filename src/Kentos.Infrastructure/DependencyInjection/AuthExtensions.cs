using System.Text;
using Kentos.Infrastructure.Authorization;
using Kentos.Infrastructure.Options;
using Kentos.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Kentos.Infrastructure.DependencyInjection;

/// <summary>Self-issued JWT authentication + role-resolved permission authorization wiring.</summary>
public static class AuthExtensions
{
    public static IServiceCollection AddKentosAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles",
                };
            });

        return services;
    }

    public static IServiceCollection AddKentosAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Fail-closed default; the Hesap module registers a DB-backed resolver that wins
        // (it registers later, and GetRequiredService returns the last registration).
        services.AddSingleton<IPermissionResolver, DenyAllPermissionResolver>();

        services.AddAuthorization();
        return services;
    }
}
