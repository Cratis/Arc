// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Registers explicitly configured verified caller resolution.</summary>
public static class VerifiedCallerServiceCollectionExtensions
{
    /// <summary>Installs verified callers using an explicit, validated browser-hop configuration.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="builder">The installation builder.</param>
    /// <returns>The same collection for chained registration.</returns>
    /// <exception cref="InvalidOperationException">The installation is incomplete or already registered.</exception>
    public static IServiceCollection AddVerifiedCallers(this IServiceCollection services, VerifiedCallersBuilder builder)
    {
        builder.Install(services);
        return services;
    }

    /// <summary>Adds the scoped accessor and fail-closed installation defaults.</summary>
    /// <param name="services">The receiving services.</param>
    /// <returns>The same service collection.</returns>
    internal static IServiceCollection AddVerifiedCallers(this IServiceCollection services)
    {
        // Deliberately inert defaults: no installation binding and no production credential source until the contract locks.
        services.TryAddSingleton(new VerifiedCallerInstallation(null, null));
        services.TryAddSingleton(new CallerResourceCatalog(new Dictionary<string, string>()));
        services.TryAddSingleton<CredentialPresentationGate>();
        services.TryAddSingleton<VerifiedCallerBuilder>();
        services.TryAddScoped<IVerifiedCallerAccessor, VerifiedCallerResolver>();
        return services;
    }
}
