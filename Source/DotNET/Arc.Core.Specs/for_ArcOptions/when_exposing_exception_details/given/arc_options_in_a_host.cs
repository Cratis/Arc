// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.for_ArcOptions.when_exposing_exception_details.given;

public abstract class arc_options_in_a_host : Specification
{
    protected bool _exposed;

    protected abstract string EnvironmentName { get; }

    /// <summary>
    /// Configures the services before Arc core services are added.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    protected virtual void ConfigureBefore(IServiceCollection services)
    {
    }

    /// <summary>
    /// Configures the services after Arc core services are added.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    protected virtual void ConfigureAfter(IServiceCollection services)
    {
    }

    protected void ResolveOptions()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(EnvironmentName);

        var services = new ServiceCollection();
        services.AddSingleton(environment);
        services.AddOptions<ArcOptions>();
        ConfigureBefore(services);
        services.AddCratisArcCore();
        ConfigureAfter(services);

        using var provider = services.BuildServiceProvider();
        _exposed = provider.GetRequiredService<IOptions<ArcOptions>>().Value.ExposeExceptionDetails;
    }
}
