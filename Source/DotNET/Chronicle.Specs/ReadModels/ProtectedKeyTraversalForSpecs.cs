// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels;

internal static class ProtectedKeyTraversalForSpecs
{
    internal static ProtectedReadModelKeyTraversal Create(ProtectedReadModelKeyOptions? options = null) =>
        new(Options.Create(options ?? new() { Strict = false }), NullLogger<ProtectedReadModelKeyTraversal>.Instance);

    internal static IServiceCollection AddProtectedKeyTraversal(this IServiceCollection services, ProtectedReadModelKeyOptions? options = null) =>
        services.AddSingleton(Create(options)).AddCompatibilityConfiguration();

    /// <summary>
    /// Opts the specifications into the reflection compatibility path, which is no longer the default.
    /// </summary>
    /// <param name="services">The collection receiving the compatibility configuration.</param>
    /// <returns>The same collection, for chaining.</returns>
    internal static IServiceCollection AddCompatibilityConfiguration(this IServiceCollection services)
    {
        services.TryAddSingleton(CompatibilityConfiguration());
        return services;
    }

    /// <summary>
    /// Creates a configuration that opts into the reflection compatibility path.
    /// </summary>
    /// <returns>The configuration.</returns>
    internal static IConfiguration CompatibilityConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReadProtection:RequireGeneratedMetadata"] = "false"
        }).Build();

    /// <summary>
    /// Gives a command context a provider carrying the reflection compatibility configuration.
    /// </summary>
    /// <param name="context">The context to extend.</param>
    /// <returns>The context with a provider.</returns>
    internal static CommandContext WithCompatibilityConfiguration(this CommandContext context) =>
        context with { ServiceProvider = new ServiceCollection().AddCompatibilityConfiguration().BuildServiceProvider() };
}
