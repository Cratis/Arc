// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// A builder for Arc applications.
/// </summary>
/// <remarks>
/// <para>
/// Initializes a new instance of the <see cref="ArcApplicationBuilder"/> class.
/// </para>
/// <para>
/// The builder starts out with a <see cref="DefaultServiceProviderFactory"/> that has
/// <see cref="ServiceProviderOptions.ValidateOnBuild"/> off and <see cref="ServiceProviderOptions.ValidateScopes"/>
/// taken from the environment, exactly as the host would derive it. Arc supplies registrations contextually —
/// <see cref="IHostApplicationBuilder"/>, the type a convention binding is for, values only an executing command
/// can hand over — and eager validation constructs every registration up front and can resolve none of them,
/// so <see cref="Build"/> would fail outright in Development, where the host turns eager validation on.
/// </para>
/// <para>
/// This is applied while the builder is being constructed, so it is a default rather than an override: any
/// <see cref="ConfigureContainer{TContainerBuilder}"/> call an application makes — before or after
/// <c>AddCratisArc</c> — replaces it and wins.
/// </para>
/// </remarks>
/// <param name="args">Command line arguments.</param>
public class ArcApplicationBuilder(string[]? args = null) : IHostApplicationBuilder
{
    readonly HostApplicationBuilder _hostBuilder = CreateHostBuilder(args);

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
        var options = host.Services.GetRequiredService<IOptions<ArcOptions>>();
        return new ArcApplication(host, options.Value);
    }

    /// <inheritdoc/>
    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull
    {
        _hostBuilder.ConfigureContainer(factory, configure);
    }

    static HostApplicationBuilder CreateHostBuilder(string[]? args)
    {
        var hostBuilder = Host.CreateApplicationBuilder(args ?? []);
        hostBuilder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateScopes = hostBuilder.Environment.IsDevelopment(),
            ValidateOnBuild = false
        }));

        return hostBuilder;
    }
}
