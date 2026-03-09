// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.Tenancy;
using Cratis.Chronicle.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IArcBuilder"/> for adding Chronicle support.
/// </summary>
public static class ArcBuilderExtensions
{
    /// <summary>
    /// Adds Chronicle support to Arc.
    /// </summary>
    /// <param name="builder">The <see cref="IArcBuilder"/> to add to.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="ChronicleAspNetCoreOptions"/>.</param>
    /// <param name="configureChronicleBuilder">Optional callback for configuring the <see cref="IChronicleBuilder"/>.</param>
    /// <returns><see cref="IArcBuilder"/> for continuation.</returns>
    public static IArcBuilder WithChronicle(
        this IArcBuilder builder,
        Action<ChronicleAspNetCoreOptions>? configureOptions = default,
        Action<IChronicleBuilder>? configureChronicleBuilder = default)
    {
        builder.Services.AddAggregateRoots(builder.Types);

        ((WebApplicationBuilder)builder.AppBuilder).AddCratisChronicle(
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

        return builder;
    }
}
