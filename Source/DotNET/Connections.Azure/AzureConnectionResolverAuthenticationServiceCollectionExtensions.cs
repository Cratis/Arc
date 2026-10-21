// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.Connections.Azure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> for authenticating the HTTP connection resolver with Microsoft Entra ID.
/// </summary>
public static class AzureConnectionResolverAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Let the HTTP connection resolver authenticate with <see cref="HttpConnectionResolverAuthenticationType.AzureCredential"/>,
    /// getting Microsoft Entra ID tokens from a managed identity, a workload identity or the default Azure credential.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// Safe to call more than once.
    /// </remarks>
    public static IServiceCollection AddAzureConnectionResolverAuthentication(this IServiceCollection services)
    {
        services.AddConnectionResolution();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConnectionResolverAccessTokenProvider, AzureCredentialAccessTokenProvider>(
            _ => new AzureCredentialAccessTokenProvider()));
        return services;
    }
}
