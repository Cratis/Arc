// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Arc.EntityFrameworkCore.Mapping;
using Cratis.Arc.EntityFrameworkCore.Observe;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Extensions for working with read-only DbContexts.
/// </summary>
public static class ReadOnlyDbContextExtensions
{
    static readonly ReadOnlySaveChangesInterceptor _readOnlySaveChangesInterceptor = new();

    /// <summary>
    /// Adds a read-only DbContext to the service collection using a pooled factory pattern.
    /// This allows multiple database providers to be used in the same application with improved performance.
    /// The pooled factory reuses internal service providers and DbContext instances.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection to add the DbContext to.</param>
    /// <param name="optionsAction">An optional action to configure the DbContext options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReadOnlyDbContext<TContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = default)
        where TContext : DbContext
    {
        // Register required application services for BaseDbContext
        services.TryAddSingleton<IEntityTypeRegistrar, EntityTypeRegistrar>();

        services.AddPooledDbContextFactory<TContext>((serviceProvider, options) =>
        {
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            // Add read-only interceptor
            options.AddInterceptors(_readOnlySaveChangesInterceptor);

            // Add ConceptAs support for handling ConceptAs types in LINQ queries
            options.AddConceptAsSupport();

            optionsAction?.Invoke(serviceProvider, options);

            if (serviceProvider.GetService<IEntityChangeTracker>() is not null)
            {
                options.AddObservation(serviceProvider);
            }
        });

        services.AddScoped(serviceProvider =>
        {
            var factory = serviceProvider.GetRequiredService<IDbContextFactory<TContext>>();
            return factory.CreateDbContext();
        });

        return services;
    }

    /// <summary>
    /// Adds a read-only DbContext to the service collection, configured with the provided connection string
    /// and database type.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection to add the DbContext to.</param>
    /// <param name="connectionString">The connection string to use for the DbContext.</param>
    /// <param name="optionsAction">An optional action to configure the DbContext options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddReadOnlyDbContextWithConnectionString<TDbContext>(this IServiceCollection services, string connectionString, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = default)
        where TDbContext : BaseDbContext
    {
        services.AddReadOnlyDbContext<TDbContext>((serviceProvider, builder) =>
        {
            builder.UseDatabaseFromConnectionString(connectionString);
            optionsAction?.Invoke(serviceProvider, builder);
        });

        return services;
    }
}
