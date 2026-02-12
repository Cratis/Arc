// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for <see cref="IEntityFrameworkCoreBuilder"/>.
/// </summary>
public static class EntityFrameworkCoreBuilderExtensions
{
    static readonly MethodInfo _addDbContextWithConnectionStringMethod =
        typeof(DbContextServiceCollectionExtensions).GetMethod(
            nameof(DbContextServiceCollectionExtensions.AddDbContextWithConnectionString),
            BindingFlags.Static | BindingFlags.Public)!;

    static readonly MethodInfo _addReadOnlyDbContextWithConnectionStringMethod =
        typeof(ReadOnlyDbContextExtensions).GetMethod(
            nameof(ReadOnlyDbContextExtensions.AddReadOnlyDbContextWithConnectionString),
            BindingFlags.Static | BindingFlags.Public)!;

    /// <summary>
    /// Adds a DbContext with the connection string from the options.
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext to add.</typeparam>
    /// <param name="builder">The <see cref="IEntityFrameworkCoreBuilder"/>.</param>
    /// <param name="optionsAction">An optional action to configure the DbContext options.</param>
    /// <returns>The builder for chaining.</returns>
    public static IEntityFrameworkCoreBuilder AddDbContext<TDbContext>(
        this IEntityFrameworkCoreBuilder builder,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = default)
        where TDbContext : DbContext
    {
        builder.Services.AddDbContextWithConnectionString<TDbContext>(builder.Options.ConnectionString, optionsAction);
        return builder;
    }

    /// <summary>
    /// Adds a DbContext with a specific connection string.
    /// </summary>
    /// <typeparam name="TDbContext">The type of DbContext to add.</typeparam>
    /// <param name="builder">The <see cref="IEntityFrameworkCoreBuilder"/>.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <param name="optionsAction">An optional action to configure the DbContext options.</param>
    /// <returns>The builder for chaining.</returns>
    public static IEntityFrameworkCoreBuilder AddDbContext<TDbContext>(
        this IEntityFrameworkCoreBuilder builder,
        string connectionString,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = default)
        where TDbContext : DbContext
    {
        builder.Services.AddDbContextWithConnectionString<TDbContext>(connectionString, optionsAction);
        return builder;
    }

    /// <summary>
    /// Discovers and registers all DbContext types that inherit from <see cref="BaseDbContext"/>.
    /// Types inheriting from <see cref="ReadOnlyDbContext"/> are registered as read-only.
    /// Types marked with <see cref="IgnoreAutoRegistrationAttribute"/> are excluded.
    /// </summary>
    /// <param name="builder">The <see cref="IEntityFrameworkCoreBuilder"/>.</param>
    /// <returns>The builder for chaining.</returns>
    public static IEntityFrameworkCoreBuilder DiscoverAndRegisterDbContexts(this IEntityFrameworkCoreBuilder builder)
    {
        var connectionString = builder.Options.ConnectionString;
        if (string.IsNullOrEmpty(connectionString))
        {
            return builder;
        }

        // Find all BaseDbContext types (which includes ReadOnlyDbContext subtypes)
        var allDbContextTypes = builder.Types
            .FindMultiple<BaseDbContext>()
            .Where(t => t.IsPublic && !t.HasAttribute<IgnoreAutoRegistrationAttribute>())
            .ToList();

        // Separate into read-only and read-write types
        var readOnlyTypes = allDbContextTypes.Where(t => typeof(ReadOnlyDbContext).IsAssignableFrom(t)).ToList();
        var readWriteTypes = allDbContextTypes.Except(readOnlyTypes).ToList();

        // Register read-only DbContexts
        foreach (var dbContextType in readOnlyTypes)
        {
            _addReadOnlyDbContextWithConnectionStringMethod
                .MakeGenericMethod(dbContextType)
                .Invoke(null, [builder.Services, connectionString, null]);
        }

        // Register read-write DbContexts
        foreach (var dbContextType in readWriteTypes)
        {
            _addDbContextWithConnectionStringMethod
                .MakeGenericMethod(dbContextType)
                .Invoke(null, [builder.Services, connectionString, null]);
        }

        return builder;
    }
}
