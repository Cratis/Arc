// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Chronicle;
using Cratis.Chronicle.Identities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Chronicle.Identities;

/// <summary>
/// Registers Arc's principal fallback for Chronicle identity resolution.
/// </summary>
public static class IdentityProviderServiceCollectionExtensions
{
    /// <summary>
    /// Wraps Chronicle's selected identity provider when its client is constructed, after DI becomes available.
    /// </summary>
    /// <param name="services">The application's services, after Chronicle registration.</param>
    /// <param name="builder">The configured Chronicle builder.</param>
    /// <param name="defaultProvider">Creates the host's default identity provider when none was configured.</param>
    /// <param name="hasHttpRequest">Checks the host's HTTP context without depending on Arc middleware having run.</param>
    public static void AddArcIdentityProvider(
        this IServiceCollection services,
        IChronicleBuilder builder,
        Func<IServiceProvider, IIdentityProvider> defaultProvider,
        Func<IServiceProvider, bool> hasHttpRequest)
    {
        // Both Chronicle host integrations register the client with a factory. Keep that factory intact;
        // only supply its structural identity dependency before it constructs the client and event stores.
        var clientFactory = services.Last(_ => _.ServiceType == typeof(IChronicleClient)).ImplementationFactory!;
        var configuredProvider = builder.IdentityProvider;
        services.AddSingleton(serviceProvider => new ArcIdentityProvider(
            configuredProvider ?? defaultProvider(serviceProvider),
            serviceProvider.GetRequiredService<ICurrentPrincipalAccessor>(),
            () => hasHttpRequest(serviceProvider)));
        services.Replace(ServiceDescriptor.Singleton<IChronicleClient>(serviceProvider =>
        {
            builder.IdentityProvider = serviceProvider.GetRequiredService<ArcIdentityProvider>();
            return (IChronicleClient)clientFactory(serviceProvider);
        }));
    }
}
