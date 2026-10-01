// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Registers convention services that the container can construct.
/// </summary>
internal static class ArcServiceBindings
{
    /// <summary>
    /// Adds constructible convention and self bindings without changing explicit registrations.
    /// </summary>
    /// <param name="services">The services to register bindings with.</param>
    /// <returns>The service collection for continuation.</returns>
    internal static IServiceCollection AddArcServiceBindings(this IServiceCollection services)
    {
        var existingCount = services.Count;
        services.AddBindingsByConvention().AddSelfBindings();

        // Fundamentals can supply enum implementation types, which Microsoft DI cannot activate.
        // Filter only the descriptors it just added; explicit caller registrations remain untouched.
        for (var index = services.Count - 1; index >= existingCount; index--)
        {
            if (services[index].ImplementationType?.IsEnum == true)
            {
                services.RemoveAt(index);
            }
        }

        return services;
    }
}
