// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Tenancy;
using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="IArcBuilder"/> for adding Chronicle support.
/// </summary>
public static class ArcBuilderExtensions
{
    /// <summary>
    /// Adds the Chronicle event store client to an Arc application, so commands and queries can append and read
    /// events. Chronicle becomes tenant-aware automatically, scoping every event store to the active tenant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wires the Chronicle client only — it connects (over gRPC, using the connection string from
    /// configuration) to a Chronicle instance that runs on its own, typically the <c>cratis/chronicle</c>
    /// container. It does not start an event store inside your process.
    /// </para>
    /// <para>
    /// Call this on the <see cref="IArcBuilder"/> from <c>AddCratisArc</c> when you want event sourcing but are
    /// wiring authentication yourself. <c>AddCratis</c> composes it for you as part of the all-in-one setup. Once
    /// added, activate it with <c>UseCratisChronicle</c> on the built application (or <c>UseCratis</c>, which
    /// activates both halves).
    /// </para>
    /// </remarks>
    /// <param name="builder">The <see cref="IArcBuilder"/> to add to.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="ChronicleClientOptions"/>.</param>
    /// <param name="configureChronicleBuilder">Optional callback for configuring the <see cref="IChronicleBuilder"/>.</param>
    /// <returns><see cref="IArcBuilder"/> for continuation.</returns>
    public static IArcBuilder WithChronicle(
        this IArcBuilder builder,
        Action<ChronicleClientOptions>? configureOptions = default,
        Action<IChronicleBuilder>? configureChronicleBuilder = default)
    {
        builder.Services.AddAggregateRoots(builder.Types);

        builder.AppBuilder.AddCratisChronicle(
            configureOptions: options =>
            {
                options.EventStoreNamespaceResolverType = typeof(TenantNamespaceResolver);
                configureOptions?.Invoke(options);
            },
            configure: chronicleBuilder =>
            {
                configureChronicleBuilder?.Invoke(chronicleBuilder);
                builder.Services.AddReadModels(chronicleBuilder.ClientArtifactsProvider);
            });

        builder.Services.AddCommandTransactions();

        return builder;
    }
}
