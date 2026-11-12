// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Choreography.Availability;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        services.AddAvailability(configureEngine);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IAvailabilityGate, ArcAvailabilityGate>();
        services.TryAddScoped<IAvailabilitySubjectProvider, AvailabilitySubjectProvider>();
        services.TryAddScoped<AvailabilityRequestContext>();
        services.TryAddScoped<IUnavailableParts, UnavailableParts>();
        return services;
    }
}
