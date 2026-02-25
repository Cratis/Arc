// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Identity;
using Cratis.Types;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up the identity provider.
/// </summary>
public static class IdentityProviderServiceCollectionExtensions
{
    /// <summary>
    /// Add an identity provider to the service collection by resolving provider type from <see cref="ArcOptions"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <exception cref="MultipleIdentityDetailsProvidersFound">Thrown if multiple identity details providers are found when no explicit provider is configured.</exception>
    public static IServiceCollection AddIdentityProvider(this IServiceCollection services)
    {
        services.AddScoped(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ArcOptions>>().Value;
            var providerType = options.IdentityDetailsProvider
                ?? ResolveIdentityDetailsProviderType(serviceProvider.GetRequiredService<ITypes>());

            TypeIsNotAnIdentityDetailsProvider.ThrowIfNotAnIdentityDetailsProvider(providerType);
            return (IProvideIdentityDetails)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, providerType);
        });

        return services;
    }

    /// <summary>
    /// Add a identity provider to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="types"><see cref="ITypes"/> for discovering identity provider.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <exception cref="MultipleIdentityDetailsProvidersFound">Thrown if multiple identity details providers are found.</exception>
    public static IServiceCollection AddIdentityProvider(this IServiceCollection services, ITypes types) =>
        services.AddIdentityProvider(ResolveIdentityDetailsProviderType(types));

    /// <summary>
    /// Add a identity provider to the service collection.
    /// </summary>
    /// <typeparam name="TProvider">The <see cref="Type"/> of the <see cref="IProvideIdentityDetails"/> implementation to add.</typeparam>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddIdentityProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IProvideIdentityDetails =>
        services.AddIdentityProvider(typeof(TProvider));

    /// <summary>
    /// Add a identity provider to the service collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="type">The <see cref="Type"/> of the <see cref="IProvideIdentityDetails"/> implementation to add.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddIdentityProvider(this IServiceCollection services, Type type)
    {
        TypeIsNotAnIdentityDetailsProvider.ThrowIfNotAnIdentityDetailsProvider(type);
        return services.AddScoped(typeof(IProvideIdentityDetails), type);
    }

    static Type ResolveIdentityDetailsProviderType(ITypes types)
    {
        var defaultImplementationType = typeof(DefaultIdentityDetailsProvider);
        var providerTypes = types.FindMultiple<IProvideIdentityDetails>().Where(_ => _ != defaultImplementationType).ToArray();
        if (providerTypes.Length > 1)
        {
            throw new MultipleIdentityDetailsProvidersFound(providerTypes);
        }

        return providerTypes.Length == 1 ? providerTypes[0] : defaultImplementationType;
    }
}
