// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Http;
using Cratis.Arc.Introspection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Maps the developer endpoints for exploring availability live, under <c>/.cratis/availability</c>.
/// </summary>
/// <param name="development">The <see cref="DevelopmentAvailability"/> holding the overrides.</param>
public class AvailabilityDevelopmentEndpoints(DevelopmentAvailability development) : IDiscoveryEndpointContributor
{
    /// <summary>
    /// The route that reports what the application declares and what is switched.
    /// </summary>
    public const string ModelRoute = "/.cratis/availability/model";

    /// <summary>
    /// The route that replaces what is switched.
    /// </summary>
    public const string OverridesRoute = "/.cratis/availability/overrides";

    const string ModelEndpointName = "IntrospectAvailabilityModel";
    const string OverridesEndpointName = "OverrideAvailability";
    const string Tag = "Cratis Availability Development";

    /// <inheritdoc/>
    public void Map(IEndpointMapper mapper, DiscoveryEndpointMetadata metadata)
    {
        // Outside a local or test environment nothing is mapped, so there is nothing to find and nothing to switch.
        if (!development.IsActive)
        {
            return;
        }

        if (!mapper.EndpointExists(ModelEndpointName))
        {
            mapper.MapGet(
                ModelRoute,
                async context =>
                {
                    var services = context.RequestServices;
                    var model = AvailabilityModel.From(
                        development.Current,
                        services.GetService<IDeclaredAudiences>(),
                        services.GetService<IAvailabilityManifest>());
                    await context.WriteResponseAsJson(model, typeof(AvailabilityModel), context.RequestAborted);
                },
                metadata.For(ModelEndpointName, "Read the availability declared by the application and what is switched", Tag, typeof(AvailabilityModel)));
        }

        if (!mapper.EndpointExists(OverridesEndpointName))
        {
            mapper.MapPost(
                OverridesRoute,
                async context =>
                {
                    if (await context.ReadBodyAsJson(typeof(AvailabilityOverrides), context.RequestAborted) is not AvailabilityOverrides overrides)
                    {
                        context.SetStatusCode(400);
                        return;
                    }

                    await development.Apply(overrides);
                    await context.WriteResponseAsJson(development.Current, typeof(AvailabilityOverrides), context.RequestAborted);
                },
                metadata.For(OverridesEndpointName, "Switch audiences, modules, features and slices on or off", Tag, typeof(AvailabilityOverrides)));
        }
    }
}
