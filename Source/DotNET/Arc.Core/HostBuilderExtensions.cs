// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Arc.Tenancy;
using Cratis.Conversion;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Cratis.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Default configuration section paths for Arc options.
    /// </summary>
    public static readonly string[] DefaultSectionPaths = ["Cratis", "Arc"];

    /// <summary>
    /// Add Cratis Arc core services with the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to extend.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="ArcOptions"/>.</param>
    /// <param name="configureBuilder">Callback for configuring the <see cref="IArcBuilder"/>.</param>
    /// <param name="configSectionPath">The optional configuration section path.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    public static IHostBuilder AddCratisArcCore(
        this IHostBuilder builder,
        Action<ArcOptions>? configureOptions = default,
        Action<IArcBuilder>? configureBuilder = default,
        string? configSectionPath = default)
    {
        builder.ConfigureServices((context, services) =>
        {
            var configSection = configSectionPath ?? ConfigurationPath.Combine(DefaultSectionPaths);
            services.Configure<ArcOptions>(context.Configuration.GetSection(configSection));

            var optionsBuilder = services.AddOptions<ArcOptions>()
                .ValidateOnStart();

            if (configureOptions is not null)
            {
                optionsBuilder.Configure(configureOptions);
            }

            services.AddCratisArcCore();

            var arcBuilder = new ArcBuilder(services, Internals.Types);
            configureBuilder?.Invoke(arcBuilder);
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
        Internals.Types = Types.Types.Instance;
        Internals.Types.RegisterTypeConvertersForConcepts();
        Internals.DerivedTypes = DerivedTypes.Instance;
        TypeConverters.Register();

        services.AddSingleton<ICorrelationIdAccessor>(sp => new CorrelationIdAccessor());

        services.AddTransient<HeaderTenantIdResolver>();
        services.AddTransient<QueryTenantIdResolver>();
        services.AddTransient<ClaimTenantIdResolver>();
        services.AddTransient<DevelopmentTenantIdResolver>();

        services.AddTransient<ITenantIdResolver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ArcOptions>>();
            return options.Value.Tenancy.ResolverType switch
            {
                TenantResolverType.Header => sp.GetRequiredService<HeaderTenantIdResolver>(),
                TenantResolverType.Query => sp.GetRequiredService<QueryTenantIdResolver>(),
                TenantResolverType.Claim => sp.GetRequiredService<ClaimTenantIdResolver>(),
                TenantResolverType.Development => sp.GetRequiredService<DevelopmentTenantIdResolver>(),
                _ => throw new InvalidOperationException($"Unknown tenant resolver type: {options.Value.Tenancy.ResolverType}")
            };
        });

        services
            .AddCratisArcMeter()
            .AddTypeDiscovery()
            .AddSingleton(Internals.DerivedTypes)
            .AddBindingsByConvention()
            .AddSelfBindings();

        services.AddCratisCommands();
        services.AddCratisQueries();

        return services;
    }

    /// <summary>
    /// Add the Meter for the Arc.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add the meter to.</param>
    /// <returns><see cref="IServiceCollection"/> for building continuation.</returns>
    public static IServiceCollection AddCratisArcMeter(this IServiceCollection services)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        services.TryAddKeyedSingleton(Internals.MeterName, new Meter(Internals.MeterName));
#pragma warning restore CA2000 // Dispose objects before losing scope
        return services;
    }
}
