// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Arc.Authentication;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http.Introspection;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
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

        Internals.Types = Types.Types.Instance;
        Internals.Types.RegisterTypeConvertersForConcepts();
        Internals.DerivedTypes = DerivedTypes.Instance;
        TypeConverters.Register();

        services.AddSingleton<ICorrelationIdAccessor, CorrelationIdAccessor>();

        services.AddSingleton<ITenantIdResolver>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ArcOptions>>();
            return options.Value.Tenancy.ResolverType switch
            {
                TenantResolverType.Header => ActivatorUtilities.GetServiceOrCreateInstance<HeaderTenantIdResolver>(sp),
                TenantResolverType.Query => ActivatorUtilities.GetServiceOrCreateInstance<QueryTenantIdResolver>(sp),
                TenantResolverType.Claim => ActivatorUtilities.GetServiceOrCreateInstance<ClaimTenantIdResolver>(sp),
                TenantResolverType.Development => ActivatorUtilities.GetServiceOrCreateInstance<DevelopmentTenantIdResolver>(sp),
                _ => throw new InvalidOperationException($"Unknown tenant resolver type: {options.Value.Tenancy.ResolverType}. Valid types are: Header, Query, Claim, Development")
            };
        });

        services
            .AddCratisArcMeter()
            .AddTypeDiscovery()
            .AddSingleton(Internals.DerivedTypes)
            .AddBindingsByConvention()
            .AddSelfBindings();

        services.TryAddSingleton<IAuthentication, Authentication.Authentication>();
        services.TryAddSingleton<IAuthorizationEvaluator, AuthorizationEvaluator>();
        services.AddSingleton<IAnonymousEvaluator, AnonymousEvaluator>();
        services.AddSingleton<IAuthorizationAttributeEvaluator, AuthorizationAttributeEvaluator>();
        services.TryAddSingleton<IDiscoverableValidators, DiscoverableValidators>();
        services.TryAddSingleton<IIntrospectionService, IntrospectionService>();
        services.TryAddSingleton<ITenantIdAccessor, TenantIdAccessor>();

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
