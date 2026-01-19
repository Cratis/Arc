// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Arc.EntityFrameworkCore.Mapping;
using Cratis.Arc.EntityFrameworkCore.Observe;
using Cratis.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Extension methods for adding DbContext services.
/// </summary>
public static class DbContextServiceCollectionExtensions
{
    /// <summary>
    /// Adds a DbContext with the specified connection string using a pooled factory pattern.
    /// This allows multiple database providers to be used in the same application with improved performance.
    /// The pooled factory reuses internal service providers and DbContext instances.
    /// </summary>
    /// <typeparam name="TDbContext">The type of the DbContext.</typeparam>
    /// <param name="services">The service collection to add the DbContext to.</param>
    /// <param name="connectionString">The connection string to use for the DbContext.</param>
    /// <param name="optionsAction">An optional action to configure the DbContext options.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDbContextWithConnectionString<TDbContext>(this IServiceCollection services, string connectionString, Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = default)
        where TDbContext : DbContext
    {
        // Register required application services for BaseDbContext
        services.TryAddSingleton<IEntityTypeRegistrar, EntityTypeRegistrar>();

        services.AddPooledDbContextFactory<TDbContext>((serviceProvider, options) =>
        {
            options.UseDatabaseFromConnectionString(connectionString);

            // Pass the application service provider so BaseDbContext can resolve IEntityTypeRegistrar
            options.UseApplicationServiceProvider(serviceProvider);

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
            var factory = serviceProvider.GetRequiredService<IDbContextFactory<TDbContext>>();
            return factory.CreateDbContext();
        });

        return services;
    }

    /// <summary>
    /// Adds all types found that implement ReadOnlyDbContext in the specified assemblies to the service collection, configured as
    /// read-only DbContexts.
    /// </summary>
    /// <param name="services">The service collection to add the DbContext types to.</param>
    /// <param name="optionsAction">An action to configure the DbContext options.</param>
    /// <param name="assemblies">The assemblies to scan for DbContext types.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddReadModelDbContextsFromAssemblies(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction, params Assembly[] assemblies)
    {
        var addDbContextMethod = typeof(ReadOnlyDbContextExtensions).GetMethod(nameof(ReadOnlyDbContextExtensions.AddReadOnlyDbContext), BindingFlags.Static | BindingFlags.Public)!;

        foreach (var dbContext in DiscoverAndFilterDbContextTypes<ReadOnlyDbContext>(assemblies))
        {
            addDbContextMethod.MakeGenericMethod(dbContext).Invoke(null, [services, optionsAction]);
        }
        return services;
    }

    /// <summary>
    /// Adds all types found that implement ReadOnlyDbContext in the specified assemblies to the service collection, configured with
    /// the provided connection string. The database type is inferred from the connection string.
    /// </summary>
    /// <param name="services">The service collection to add the DbContext types to.</param>
    /// <param name="connectionString">The connection string to use for the DbContext types.</param>
    /// <param name="optionsAction">An action to configure the DbContext options.</param>
    /// <param name="assemblies">The assemblies to scan for DbContext types.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <exception cref="UnsupportedDatabaseType">Thrown if the connection string does not have a supported database type.</exception>
    public static IServiceCollection AddReadModelDbContextsWithConnectionStringFromAssemblies(this IServiceCollection services, string connectionString, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction, params Assembly[] assemblies)
    {
        var addDbContextMethod = typeof(ReadOnlyDbContextExtensions).GetMethod(nameof(ReadOnlyDbContextExtensions.AddReadOnlyDbContextWithConnectionString), BindingFlags.Static | BindingFlags.Public)!;

        foreach (var dbContext in DiscoverAndFilterDbContextTypes<ReadOnlyDbContext>(assemblies))
        {
            addDbContextMethod.MakeGenericMethod(dbContext).Invoke(null, [services, connectionString, optionsAction]);
        }
        return services;
    }

    /// <summary>
    /// Discovers and filters DbContext types from the specified assemblies, excluding those marked with IgnoreAutoRegistrationAttribute.
    /// </summary>
    /// <typeparam name="TDbContext">The base DbContext type to filter for.</typeparam>
    /// <param name="assemblies">The assemblies to scan for DbContext types.</param>
    /// <returns>A collection of filtered DbContext types.</returns>
    static IEnumerable<Type> DiscoverAndFilterDbContextTypes<TDbContext>(Assembly[] assemblies)
        where TDbContext : class => Types.Types.Instance
            .FindMultiple<TDbContext>()
            .Where(t => assemblies.Contains(t.Assembly) &&
                       t.IsPublic &&
                       !t.HasAttribute<IgnoreAutoRegistrationAttribute>());
}
