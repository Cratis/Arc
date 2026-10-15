// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> for connection resolution.
/// </summary>
public static class ConnectionResolutionServiceCollectionExtensions
{
    /// <summary>
    /// Add the services for resolving connections, see <see cref="IConnectionResolverFactory"/>.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// Safe to call more than once. Storage integrations call it for you.
    /// </remarks>
    public static IServiceCollection AddConnectionResolution(this IServiceCollection services)
    {
        services.TryAddSingleton<IConnectionResolverFactory, ConnectionResolverFactory>();
        return services;
    }
}
