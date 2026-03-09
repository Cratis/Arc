// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc;

/// <summary>
/// Represents a builder for configuring Arc services or extensions.
/// </summary>
/// <param name="AppBuilder">The <see cref="IHostApplicationBuilder"/> for the application.</param>
/// <param name="Types">The types system used by Arc.</param>
public record ArcBuilder(IHostApplicationBuilder AppBuilder, ITypes Types) : IArcBuilder
{
    /// <inheritdoc/>
    public IServiceCollection Services => AppBuilder.Services;

    /// <inheritdoc/>
    public IConfigurationManager Configuration => AppBuilder.Configuration;
}
