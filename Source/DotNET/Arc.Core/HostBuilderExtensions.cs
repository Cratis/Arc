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
            .AddBindingsByConvention()
            .AddSelfBindings();

        Internals.Types = services.UseCurrentTypeUniverse();
        Internals.Types.RegisterTypeConvertersForConcepts();
        Internals.DerivedTypes = services.UseDerivedTypesFrom(Internals.Types);

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
    /// Generated type discovery providers register from module constructors, which run only when something reaches
    /// their assembly. A universe built before that has happened is missing everything a later provider brings in,
    /// and nothing about it says so - a shorter <c>ITypes.All</c> is indistinguishable from a feature nobody wrote.
    /// <see cref="TypesServiceCollectionExtensions.CurrentTypeUniverse"/> runs that provider walk itself and returns
    /// the one instance <see cref="TypesServiceCollectionExtensions.AddTypeDiscovery"/> registers, which is why
    /// <c>Types.Instance</c> is not used at all: being a static field it snapshots the provider registry the first
    /// time anything touches the type, so a later provider can never reach it.
    /// </para>
    /// <para>
    /// <c>Replace</c> rather than trusting that identity: a provider registering between <c>AddTypeDiscovery</c>
    /// and this call rebuilds the universe, and the container would otherwise hold the older one. Fundamentals
    /// 7.19.1 is the floor because earlier versions left running the walk to the caller.
    /// </para>
    /// <para>
    /// Reordering the chain to walk before <c>AddTypeDiscovery</c> would work out to the same universe, but it also
    /// exposes <c>ITypes</c> to convention binding, and a conventionally bound <c>ITypes</c> constructs a whole new
    /// universe per resolution.
    /// </para>
    /// </remarks>
    static ITypes UseCurrentTypeUniverse(this IServiceCollection services)
    {
        var current = TypesServiceCollectionExtensions.CurrentTypeUniverse();
        services.Replace(ServiceDescriptor.Singleton(current));
        return current;
    }

    /// <summary>
    /// Registers the derived types read off the given universe, replacing whatever else claimed
    /// <see cref="IDerivedTypes"/> while the collection was being built, and returns it so the caller holds the same
    /// instance the container resolves.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to register the derived types with.</param>
    /// <param name="types">The <see cref="ITypes"/> universe to read derived types off.</param>
    /// <returns>The <see cref="DerivedTypes"/> the container will hand out.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="DerivedTypes.Instance"/> is built from <c>Types.Instance</c>, so it carries the same narrowed
    /// universe taking <c>Internals.Types</c> late exists to avoid - and merely reading it pins that static to
    /// whatever the provider registry held at that moment, for the rest of the process and for every other Cratis
    /// product sharing it. Building from the universe Arc just took keeps polymorphic JSON and the MongoDB
    /// discriminator conventions seeing the same types everything else in the host sees, at the cost of no longer
    /// sharing Fundamentals' global singleton. The cost is one more pass over a universe
    /// <c>RegisterTypeConvertersForConcepts</c> has already walked in the same call.
    /// </para>
    /// <para>
    /// <c>Replace</c> rather than <c>AddSingleton</c> because <c>AddBindingsByConvention</c> binds
    /// <see cref="IDerivedTypes"/> to <see cref="DerivedTypes"/> by convention unless the service type is already
    /// registered, and it now runs first. A conventionally bound one would resolve off the container's
    /// <see cref="ITypes"/> to an instance that is equivalent but not the one Arc itself holds, which is the
    /// divergence between <c>Internals</c> and the container this whole sequence exists to close.
    /// </para>
    /// </remarks>
    static DerivedTypes UseDerivedTypesFrom(this IServiceCollection services, ITypes types)
    {
        var current = new DerivedTypes(types);
        services.Replace(ServiceDescriptor.Singleton<IDerivedTypes>(current));
        return current;
    }
}
