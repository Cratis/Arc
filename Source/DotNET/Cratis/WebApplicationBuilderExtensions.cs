// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;

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
    /// <param name="configureArcBuilder">An optional action to configure the <see cref="ArcBuilder"/>.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for building continuation.</returns>
    public static WebApplicationBuilder AddCratis(this WebApplicationBuilder builder, Action<IArcBuilder>? configureArcBuilder)
    {
        builder.AddCratisArc(
            arcBuilder =>
            {
                configureArcBuilder?.Invoke(arcBuilder);
                arcBuilder.WithChronicle();
            });
        builder.AddCratisChronicle();
        builder.UseCratisMongoDB();

        return builder;
    }
}
