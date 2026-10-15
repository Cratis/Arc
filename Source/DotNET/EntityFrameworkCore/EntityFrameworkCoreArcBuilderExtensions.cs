// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Connections;
using Cratis.Arc.EntityFrameworkCore.Json;
using Cratis.Arc.Tenancy;
using Cratis.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc;

/// <summary>
/// Provides extension methods for <see cref="IArcBuilder"/> to configure Entity Framework Core.
/// </summary>
public static class EntityFrameworkCoreArcBuilderExtensions
{
    /// <summary>
    /// Default configuration section paths for <see cref="EntityFrameworkCoreOptions"/>.
    /// </summary>
    public static readonly string[] DefaultSectionPaths = ["Cratis", "EntityFrameworkCore"];

    /// <summary>
    /// Add Entity Framework Core support to the solution.
    /// </summary>
    /// <param name="arcBuilder"><see cref="IArcBuilder"/> to use Entity Framework Core with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="EntityFrameworkCoreOptions"/>.</param>
    /// <param name="configureEfCore">The optional callback for configuring <see cref="IEntityFrameworkCoreBuilder"/>.</param>
    /// <returns><see cref="IArcBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// <see cref="EntityFrameworkCoreOptions"/> are bound from the <c>Cratis:EntityFrameworkCore</c> configuration section
    /// first; <paramref name="configureOptions"/> runs after and wins.
    /// </remarks>
    public static IArcBuilder WithEntityFrameworkCore(
        this IArcBuilder arcBuilder,
        Action<EntityFrameworkCoreOptions>? configureOptions = default,
        Action<IEntityFrameworkCoreBuilder>? configureEfCore = default)
    {
        // Add observation services first to ensure singleton registration takes precedence
        arcBuilder.Services.AddEntityFrameworkCoreObservation();

        var builder = new EntityFrameworkCoreBuilder(arcBuilder.Services, arcBuilder.Types);

        arcBuilder.Configuration.GetSection(ConfigurationPath.Combine(DefaultSectionPaths)).Bind(builder.Options);
        configureOptions?.Invoke(builder.Options);
        configureEfCore?.Invoke(builder);

        // Build and register JsonConversionOptions from the configured EF Core options so that
        // BaseDbContext (and direct callers) can resolve the final JsonSerializerOptions from DI.
        // The factory resolves IDerivedTypes from DI so that polymorphic types registered via
        // DerivedTypeJsonConverterFactory are handled correctly when model creation runs.
        var userConverters = builder.Options.JsonConverters.ToList();
        arcBuilder.Services.TryAddSingleton(sp =>
        {
            var derivedTypes = sp.GetService<IDerivedTypes>();
            var opts = new JsonConversionOptions(derivedTypes);
            foreach (var converter in userConverters)
            {
                opts.JsonSerializerOptions.Converters.Add(converter);
            }
            return opts;
        });

        AddConnectionResolution(arcBuilder.Services, builder.Options.ConnectionResolution);

        // Auto-discover and register DbContext types if enabled
        if (builder.Options.AutoDiscoverDbContexts && !string.IsNullOrEmpty(builder.Options.ConnectionString))
        {
            builder.DiscoverAndRegisterDbContexts();
        }

        return arcBuilder;
    }

    static void AddConnectionResolution(IServiceCollection services, ConnectionResolutionOptions options)
    {
        if (!options.IsConfigured)
        {
            return;
        }

        services.AddConnectionResolution();
        services.TryAddSingleton(sp => new ConnectionResolutionInterceptor(
            sp.GetRequiredService<IConnectionResolverFactory>().Create(options),
            sp.GetRequiredService<ITenantIdAccessor>()));
    }
}
