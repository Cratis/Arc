// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>
/// A builder for Arc applications.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ArcApplicationBuilder"/> class.
/// </remarks>
/// <param name="args">Command line arguments.</param>
public class ArcApplicationBuilder(string[]? args = null)
{
    readonly HostApplicationBuilder _hostBuilder = Host.CreateApplicationBuilder(args ?? []);

    /// <summary>
    /// Gets the configuration manager.
    /// </summary>
    public IConfigurationManager Configuration => _hostBuilder.Configuration;

    /// <summary>
    /// Gets the host environment.
    /// </summary>
    public IHostEnvironment Environment => _hostBuilder.Environment;

    /// <summary>
    /// Gets the logging builder.
    /// </summary>
    public ILoggingBuilder Logging => _hostBuilder.Logging;

    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceCollection Services => _hostBuilder.Services;

    /// <summary>
    /// Gets the metrics builder.
    /// </summary>
    public IMetricsBuilder Metrics => _hostBuilder.Metrics;

    /// <summary>
    /// Builds the <see cref="ArcApplication"/>.
    /// </summary>
    /// <returns>A configured <see cref="ArcApplication"/>.</returns>
    public ArcApplication Build()
    {
        var host = _hostBuilder.Build();
        return new ArcApplication(host);
    }
}
