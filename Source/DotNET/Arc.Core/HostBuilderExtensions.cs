// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Identity;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Cratis.Conversion;
using Cratis.DependencyInjection;
using Cratis.Execution;
using Cratis.Serialization;
using Cratis.Types;
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

            services.AddOptions<ArcOptions>()
                .ValidateOnStart();

            if (configureOptions is not null)
            {
                services.PostConfigure(configureOptions);
            }

            services.AddCratisArcCore();
            services.AddIdentityProvider();

            if (configureBuilder is not null)
            {
                throw new NotSupportedException(
                    "The IHostBuilder path does not support configureBuilder because IHostApplicationBuilder is not available. " +
                    "Use WebApplicationBuilder.AddCratisArc() or ArcApplicationBuilder.AddCratisArc() instead.");
            }
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
        GeneratedMetadataRegistration.EnsureGeneratedMetadataRegistered();

        Internals.DerivedTypes = DerivedTypes.Instance;
        TypeConverters.Register();

        services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<ArcOptions>, TenancyOptionsValidator>());

        services.AddSingleton<CurrentPrincipalAccessor>();
        services.AddSingleton<ICurrentPrincipalAccessor>(sp => sp.GetRequiredService<CurrentPrincipalAccessor>());
        services.AddSingleton<ICurrentPrincipalOverride>(sp => sp.GetRequiredService<CurrentPrincipalAccessor>());

        services.AddSingleton<ITenantIdResolver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ArcOptions>>();
            return options.Value.Tenancy.ResolverType switch
            {
                TenantResolverType.Subdomain => ActivatorUtilities.GetServiceOrCreateInstance<SubdomainTenantIdResolver>(sp),
                TenantResolverType.Header => ActivatorUtilities.GetServiceOrCreateInstance<HeaderTenantIdResolver>(sp),
                TenantResolverType.Query => ActivatorUtilities.GetServiceOrCreateInstance<QueryTenantIdResolver>(sp),
                TenantResolverType.Claim => ActivatorUtilities.GetServiceOrCreateInstance<ClaimTenantIdResolver>(sp),
                TenantResolverType.Development => ActivatorUtilities.GetServiceOrCreateInstance<DevelopmentTenantIdResolver>(sp),
                TenantResolverType.Fixed => ActivatorUtilities.GetServiceOrCreateInstance<FixedTenantIdResolver>(sp),
                _ => throw new InvalidOperationException($"Unknown tenant resolver type: {options.Value.Tenancy.ResolverType}. Valid types are: Header, Query, Claim, Development, Subdomain, Fixed")
            };
        });

        services
            .AddCratisArcMeter()
            .AddCratisArcActivitySource()
            .AddTypeDiscovery()
            .AddSingleton(Internals.DerivedTypes)
            .AddBindingsByConvention()
            .AddSelfBindings();

        Internals.Types = services.UseCurrentTypeUniverse();
        Internals.Types.RegisterTypeConvertersForConcepts();

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
        services.TryAddKeyedSingleton(Internals.MeterName, (_, _) => new Meter(Internals.MeterName));
        return services;
    }

    /// <summary>
    /// Add the ActivitySource for the Arc.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add the activity source to.</param>
    /// <returns><see cref="IServiceCollection"/> for building continuation.</returns>
    public static IServiceCollection AddCratisArcActivitySource(this IServiceCollection services)
    {
        return services
            .AddActivitySource(Internals.ActivitySourceName)
            .AddActivitySource<CommandFilters>(Internals.ActivitySourceName)
            .AddActivitySource<CommandPipeline>(Internals.ActivitySourceName)
            .AddActivitySource<QueryFilters>(Internals.ActivitySourceName)
            .AddActivitySource<QueryPipeline>(Internals.ActivitySourceName)
            .AddActivitySource<IdentityProvider>(Internals.ActivitySourceName);
    }

    /// <summary>
    /// Registers the type universe as it stands once every generated type discovery provider has registered,
    /// replacing the one <see cref="TypesServiceCollectionExtensions.AddTypeDiscovery"/> registered earlier, and
    /// returns it so the caller holds the same instance the container resolves.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to register the universe with.</param>
    /// <returns>The <see cref="ITypes"/> the container will hand out.</returns>
    /// <remarks>
    /// <para>
    /// <c>AddBindingsByConvention</c> and <c>AddSelfBindings</c> walk the assembly reference closure and run module
    /// constructors, which is where generated providers for assemblies nothing had touched yet register themselves.
    /// A universe built before that walk is missing everything the walk brings in, and nothing about it says so - a
    /// shorter <c>ITypes.All</c> is indistinguishable from a feature nobody wrote. That is why the universe is taken
    /// here rather than at the top of <c>AddCratisArcCore</c>, and why <c>Types.Instance</c> is no longer used for
    /// it at all: being a static field it snapshots the provider registry the first time anything touches the type,
    /// so a later provider can never reach it.
    /// </para>
    /// <para>
    /// Asking <c>AddTypeDiscovery</c> for the universe - rather than constructing one - is what keeps this instance
    /// identical to the one a container configured after the walk resolves. Fundamentals 7.18.5 adds to that only
    /// the cost: it keys its default universe on the registered provider set, so this rebuilds while the set is
    /// still growing and is a lookup once it has settled, where 7.18.2 built an equally correct universe from
    /// scratch every call. Correctness comes from taking the universe late; the pin moves so that taking it late
    /// stays affordable for a host that configures many containers.
    /// </para>
    /// <para>
    /// Reordering the chain to walk before <c>AddTypeDiscovery</c> would work out to the same universe, but it also
    /// exposes <c>ITypes</c> and <c>IDerivedTypes</c> to convention binding, and a conventionally bound
    /// <c>ITypes</c> constructs a whole new universe per resolution.
    /// </para>
    /// </remarks>
    static ITypes UseCurrentTypeUniverse(this IServiceCollection services)
    {
        var current = (ITypes)new ServiceCollection()
            .AddTypeDiscovery()
            .Single(_ => _.ServiceType == typeof(ITypes))
            .ImplementationInstance!;

        services.Replace(ServiceDescriptor.Singleton<ITypes>(current));
        return current;
    }
}
