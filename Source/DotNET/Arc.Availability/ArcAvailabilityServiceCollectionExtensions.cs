// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Commands;
using Cratis.Arc.Contexts;
using Cratis.Arc.Introspection;
using Cratis.Arc.Queries;
using Cratis.Choreography.Availability;
using Cratis.Choreography.Availability.Audiences;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding Arc availability gates to a service collection.
/// </summary>
public static class ArcAvailabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds availability gates to the command and query pipelines, the client availability query and MCP tool listing.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">Optional configuration of gates that are not declared with attributes.</param>
    /// <param name="configureEngine">Optional configuration of the Choreography <see cref="AvailabilityOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// The host registers the Choreography pieces it owns: an <c>IAudienceSource</c>, <c>IDeclaredAudiences</c>, an
    /// <c>AvailabilityCatalog</c> and a <c>ContextValueCatalog</c>. A host that does not call this method pays nothing.
    /// </remarks>
    public static IServiceCollection AddArcAvailability(
        this IServiceCollection services,
        Action<ArcAvailabilityOptions>? configure = null,
        Action<AvailabilityOptions>? configureEngine = null)
    {
        var options = new ArcAvailabilityOptions();
        configure?.Invoke(options);

        var firstRegistration = !services.Any(_ => _.ServiceType == typeof(DevelopmentAvailability));
        services.AddAvailability(configureEngine);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IAvailabilityGate, ArcAvailabilityGate>();
        services.TryAddScoped<IAvailabilitySubjectProvider, AvailabilitySubjectProvider>();
        services.TryAddScoped<AvailabilityRequestContext>();
        services.TryAddScoped<IContextValueSource, ChoreographyContextValueSource>();
        services.TryAddScoped<IUnavailableParts, UnavailableParts>();

        if (firstRegistration)
        {
            services.AddSingleton(serviceProvider => new DevelopmentAvailability(serviceProvider.GetService<IHostEnvironment>()?.EnvironmentName));
            services.AddSingleton<IDiscoveryEndpointContributor, AvailabilityDevelopmentEndpoints>();
            Decorate<IAudienceResolver>(services, (serviceProvider, inner) => new DevelopmentAudienceResolver(inner, serviceProvider.GetRequiredService<DevelopmentAvailability>()));
            Decorate<IAvailabilityGate>(services, (serviceProvider, inner) => new DevelopmentAvailabilityGate(
                inner,
                serviceProvider.GetRequiredService<DevelopmentAvailability>(),
                serviceProvider.GetService<ICommandHandlerProviders>(),
                serviceProvider.GetService<IQueryPerformerProviders>()));
        }

        return services;
    }

    /// <summary>
    /// Replaces the last registration of a service with a wrapper around what it would have produced.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="decorator">Creates the wrapper from the services and the service it wraps.</param>
    static void Decorate<TService>(IServiceCollection services, Func<IServiceProvider, TService, TService> decorator)
        where TService : class
    {
        var descriptor = services.LastOrDefault(_ => _.ServiceType == typeof(TService) && !_.IsKeyedService);
        if (descriptor is null)
        {
            return;
        }

        services.Remove(descriptor);
        services.Add(new ServiceDescriptor(
            typeof(TService),
            serviceProvider => decorator(serviceProvider, (TService)Create(serviceProvider, descriptor)),
            descriptor.Lifetime));
    }

    static object Create(IServiceProvider serviceProvider, ServiceDescriptor descriptor) =>
        descriptor.ImplementationInstance
            ?? descriptor.ImplementationFactory?.Invoke(serviceProvider)
            ?? ActivatorUtilities.CreateInstance(serviceProvider, descriptor.ImplementationType!);
}
