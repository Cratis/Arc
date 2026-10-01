// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Registers convention services that the container can construct.
/// </summary>
internal static class ArcServiceBindings
{
    static readonly ConditionalWeakTable<IServiceCollection, HashSet<ServiceDescriptor>> _bindings = new();

    /// <summary>
    /// Adds constructible convention and self bindings without changing explicit registrations.
    /// </summary>
    /// <param name="services">The services to register bindings with.</param>
    /// <returns>The service collection for continuation.</returns>
    internal static IServiceCollection AddArcServiceBindings(this IServiceCollection services)
    {
        var bindings = _bindings.GetValue(services, _ => new(ReferenceEqualityComparer.Instance));
        var existingCount = services.Count;
        services.AddBindingsByConvention().AddSelfBindings();

        // Convention metadata can include types that Microsoft DI cannot activate.
        // Filter only the descriptors it just added; explicit caller registrations remain untouched.
        for (var index = services.Count - 1; index >= existingCount; index--)
        {
            if (services[index].ImplementationType is { } implementationType &&
                (implementationType.IsEnum || implementationType.IsInterface || implementationType.IsAbstract ||
                 implementationType.IsAssignableTo(typeof(Delegate)) ||
                 (implementationType.IsValueType && implementationType.GetConstructors().Length == 0)))
            {
                services.RemoveAt(index);
            }
            else
            {
                bindings.Add(services[index]);
            }
        }

        return services;
    }

    /// <summary>
    /// Removes only the bindings Arc added for a service, preserving caller registrations.
    /// </summary>
    /// <param name="services">The services containing the bindings.</param>
    /// <param name="serviceType">The service type whose convention bindings should be removed.</param>
    internal static void RemoveArcServiceBindingsFor(this IServiceCollection services, Type serviceType)
    {
        if (!_bindings.TryGetValue(services, out var bindings))
        {
            return;
        }

        foreach (var descriptor in bindings.Where(_ => _.ServiceType == serviceType).ToArray())
        {
            services.Remove(descriptor);
            bindings.Remove(descriptor);
        }
    }
}
