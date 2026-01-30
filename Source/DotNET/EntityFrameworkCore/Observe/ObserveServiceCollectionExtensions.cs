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
    /// Uses Replace to ensure singleton lifetime even if convention-based registration
    /// has already registered these types with a different lifetime.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEntityFrameworkCoreObservation(this IServiceCollection services)
    {
        // Use Replace to ensure singleton lifetime.
        // AddBindingsByConvention() may have already registered these with non-singleton lifetime.
        // Replace removes any existing registration and adds the new one.
        services.Replace(ServiceDescriptor.Singleton<IEntityChangeTracker, EntityChangeTracker>());
        services.Replace(ServiceDescriptor.Singleton<IServiceBrokerManager, ServiceBrokerManager>());
        services.Replace(ServiceDescriptor.Singleton<IDatabaseChangeNotifierFactory, DatabaseChangeNotifierFactory>());
        return services;
    }
}
