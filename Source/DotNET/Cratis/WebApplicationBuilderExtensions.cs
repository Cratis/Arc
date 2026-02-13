// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.Tenancy;
using Cratis.Chronicle.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="WebApplicationBuilder"/> for configuring Cratis.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Add Cratis to the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to extend.</param>
    /// <param name="configureArcOptions">An optional action to configure <see cref="ArcOptions"/>.</param>
    /// <param name="configureArcBuilder">An optional action to configure the <see cref="ArcBuilder"/>.</param>
    /// <param name="configureArcChronicleOptions">An optional action to configure <see cref="ChronicleAspNetCoreOptions"/>.</param>
    /// <param name="configureChronicleBuilder">An optional action to configure the <see cref="ChronicleBuilder"/>.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for building continuation.</returns>
    public static WebApplicationBuilder AddCratis(
        this WebApplicationBuilder builder,
        Action<ArcOptions>? configureArcOptions = default,
        Action<IArcBuilder>? configureArcBuilder = default,
        Action<ChronicleAspNetCoreOptions>? configureArcChronicleOptions = default,
        Action<IChronicleBuilder>? configureChronicleBuilder = default)
    {
        builder.AddCratisArc(
            configureOptions: configureArcOptions,
            configureBuilder: arcBuilder =>
            {
                configureArcBuilder?.Invoke(arcBuilder);
                arcBuilder.WithChronicle();
            });
        builder.AddCratisChronicle(
            configureOptions: options =>
            {
                options.EventStoreNamespaceResolverType = typeof(TenantNamespaceResolver);
                configureArcChronicleOptions?.Invoke(options);
            },
            configure: configureChronicleBuilder);

        return builder;
    }
}
