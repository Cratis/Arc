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
public class ArcApplicationBuilder(string[]? args = null) : IHostApplicationBuilder
{
    readonly HostApplicationBuilder _hostBuilder = Host.CreateApplicationBuilder(args ?? []);

    /// <inheritdoc/>
    public IConfigurationManager Configuration => _hostBuilder.Configuration;

    /// <inheritdoc/>
    public IHostEnvironment Environment => _hostBuilder.Environment;

    /// <inheritdoc/>
    public ILoggingBuilder Logging => _hostBuilder.Logging;

    /// <inheritdoc/>
    public IServiceCollection Services => _hostBuilder.Services;

    /// <inheritdoc/>
    public IMetricsBuilder Metrics => _hostBuilder.Metrics;

    /// <inheritdoc/>
    public IDictionary<object, object> Properties => ((IHostApplicationBuilder)_hostBuilder).Properties;

    /// <summary>
    /// Builds the <see cref="ArcApplication"/>.
    /// </summary>
    /// <returns>A configured <see cref="ArcApplication"/>.</returns>
    public ArcApplication Build()
    {
        var host = _hostBuilder.Build();
        return new ArcApplication(host);
    }

    /// <inheritdoc/>
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull
    {
        _hostBuilder.ConfigureContainer(factory, configure);
    }
}
