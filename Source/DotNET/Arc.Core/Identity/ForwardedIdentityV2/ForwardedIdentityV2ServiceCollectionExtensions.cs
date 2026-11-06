// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Registers the internal, explicitly configured v2 verification profile.</summary>
internal static class ForwardedIdentityV2ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the internal verification profile; activate only when the contract locks (Cratis/Identity#14).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="settings">The explicit installation settings.</param>
    /// <returns>The same collection for chained registration.</returns>
    /// <exception cref="ForwardedIdentityNonceStoreNotConfigured">No nonce store was explicitly registered.</exception>
    internal static IServiceCollection AddForwardedIdentityV2(this IServiceCollection services, ForwardedIdentityV2Settings settings)
    {
        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IForwardedIdentityNonceStore))) throw new ForwardedIdentityNonceStoreNotConfigured();
        services.AddSingleton(settings);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IForwardedBearerVerifier, ForwardedBearerVerifierAdapter>();
        services.TryAddScoped<ForwardedIdentityV2Verifier>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IVerifiedCredentialSource, ForwardedIdentityV2CredentialSource>());
        return services;
    }

    /// <summary>Explicitly selects single-process replay protection without replacing an existing store.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chained registration.</returns>
    internal static IServiceCollection AddProcessLocalForwardedIdentityNonceStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IForwardedIdentityNonceStore, ProcessLocalForwardedIdentityNonceStore>();
        return services;
    }
}
