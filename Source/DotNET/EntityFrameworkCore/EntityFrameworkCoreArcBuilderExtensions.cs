// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc;

/// <summary>
/// Provides extension methods for <see cref="IArcBuilder"/> to configure Entity Framework Core.
/// </summary>
public static class EntityFrameworkCoreArcBuilderExtensions
{
    /// <summary>
    /// Add Entity Framework Core support to the solution.
    /// </summary>
    /// <param name="arcBuilder"><see cref="IArcBuilder"/> to use Entity Framework Core with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="EntityFrameworkCoreOptions"/>.</param>
    /// <param name="configureEfCore">The optional callback for configuring <see cref="IEntityFrameworkCoreBuilder"/>.</param>
    /// <returns><see cref="IArcBuilder"/> for building continuation.</returns>
    public static IArcBuilder WithEntityFrameworkCore(
        this IArcBuilder arcBuilder,
        Action<EntityFrameworkCoreOptions>? configureOptions = default,
        Action<IEntityFrameworkCoreBuilder>? configureEfCore = default)
    {
        // Add observation services first to ensure singleton registration takes precedence
        arcBuilder.Services.AddEntityFrameworkCoreObservation();

        var builder = new EntityFrameworkCoreBuilder(arcBuilder.Services, arcBuilder.Types);

        configureOptions?.Invoke(builder.Options);
        configureEfCore?.Invoke(builder);

        // Auto-discover and register DbContext types if enabled
        if (builder.Options.AutoDiscoverDbContexts && !string.IsNullOrEmpty(builder.Options.ConnectionString))
        {
            builder.DiscoverAndRegisterDbContexts();
        }

        return arcBuilder;
    }
}
