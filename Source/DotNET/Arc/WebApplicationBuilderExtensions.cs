// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="WebApplicationBuilder"/> for configuring Arc services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Use Cratis Arc with the <see cref="WebApplicationBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Binds the <see cref="ArcOptions"/> configuration to the given config section path or the default
    /// Cratis:Arc section path.
    /// </remarks>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to extend.</param>
    /// <param name="configureOptions">The optional callback for configuring <see cref="ArcOptions"/>.</param>
    /// <param name="configureBuilder">Callback for configuring the <see cref="IArcBuilder"/>.</param>
    /// <param name="configSectionPath">The optional configuration section path.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for building continuation.</returns>
    public static WebApplicationBuilder AddCratisArc(
        this WebApplicationBuilder builder,
        Action<ArcOptions>? configureOptions = default,
        Action<IArcBuilder>? configureBuilder = default,
        string? configSectionPath = default)
    {
        builder.Host.AddCratisArc(configureOptions, configureBuilder, configSectionPath);
        return builder;
    }
}