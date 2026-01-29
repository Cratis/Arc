// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add Entity Framework Core observation support.
/// </summary>
public static class ObserveServiceCollectionExtensions
{
    /// <summary>
    /// Adds Entity Framework Core observation services to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEntityFrameworkCoreObservation(this IServiceCollection services)
    {
        services.TryAddSingleton<IEntityChangeTracker, EntityChangeTracker>();
        services.TryAddSingleton<IServiceBrokerManager, ServiceBrokerManager>();
        services.TryAddSingleton<IDatabaseChangeNotifierFactory, DatabaseChangeNotifierFactory>();
        return services;
    }
}
