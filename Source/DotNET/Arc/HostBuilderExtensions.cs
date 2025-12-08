// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Add Cratis Arc core services with the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to extend.</param>
    /// <param name="arcBuilderCallback">Callback for configuring the <see cref="IArcBuilder"/>.</param>
    /// <param name="configSectionPath">The optional configuration section path.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    public static IHostBuilder AddCratisArcCore(this IHostBuilder builder, Action<IArcBuilder> arcBuilderCallback, string? configSectionPath = null)
    {
        builder.ConfigureServices((context, services) =>
        {
            var configSection = configSectionPath ?? "Cratis:Arc";
            services.Configure<ArcOptions>(context.Configuration.GetSection(configSection));

            services.AddOptions<ArcOptions>()
                .ValidateOnStart();

            services.AddCratisArcCore();

            var arcBuilder = new ArcBuilder(services, Internals.Types);
            arcBuilderCallback(arcBuilder);
        });

        return builder;
    }

    /// <summary>
    /// Add core Cratis Arc services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisArcCore(this IServiceCollection services)
    {
        services.AddSingleton<ICorrelationIdAccessor>(sp => new CorrelationIdAccessor());
        services
            .AddTypeDiscovery()
            .AddBindingsByConvention()
            .AddSelfBindings();

        services.AddCratisCommands();
        services.AddCratisQueries();

        return services;
    }
}
