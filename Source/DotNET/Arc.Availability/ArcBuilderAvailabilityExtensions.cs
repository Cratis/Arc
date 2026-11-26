// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability;

/// <summary>
/// Provides extensions for adding availability to Arc.
/// </summary>
public static class ArcBuilderAvailabilityExtensions
{
    /// <summary>
    /// Adds availability: gates on commands and queries that refuse the parts of the application that are not available,
    /// before authorization.
    /// </summary>
    /// <param name="builder">The Arc builder.</param>
    /// <param name="configure">Optional configuration of gates that are not declared with attributes.</param>
    /// <param name="configureEngine">Optional configuration of the engine <see cref="AvailabilityOptions"/>.</param>
    /// <returns>The builder for continuation.</returns>
    /// <remarks>
    /// This is the one registration for availability. The audience source defaults to <see cref="Audiences.InMemoryAudienceSource"/>,
    /// and the declared blocks are validated at startup. Register another <see cref="Audiences.IAudienceSource"/> to keep audiences
    /// elsewhere.
    /// </remarks>
    public static IArcBuilder WithAvailability(
        this IArcBuilder builder,
        Action<ArcAvailabilityOptions>? configure = null,
        Action<AvailabilityOptions>? configureEngine = null)
    {
        builder.Services.AddArcAvailability(configure, configureEngine);
        return builder;
    }
}
