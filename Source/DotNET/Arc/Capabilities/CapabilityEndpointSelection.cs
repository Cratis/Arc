// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Compiles a startup-only snapshot of endpoints positively known to be outside the gate.
/// </summary>
/// <param name="options">The configured capability endpoint selection.</param>
[Singleton]
internal sealed class CapabilityEndpointSelection(IOptions<CapabilitiesOptions> options)
{
    FrozenSet<Endpoint>? _outside;

    /// <summary>Validates configured rules before endpoint compilation.</summary>
    /// <exception cref="CapabilityEndpointSelectorMatchesNothing">The selector is explicitly empty.</exception>
    /// <exception cref="CapabilityEndpointSelectorIsAmbiguous">Rules are duplicate or overlapping.</exception>
    public void Validate()
    {
        var selector = options.Value.GatedEndpoints;
        if (selector is null)
        {
            return;
        }
        if (selector.RoutePatterns.Count == 0 && selector.MetadataTypes.Count == 0)
        {
            throw new CapabilityEndpointSelectorMatchesNothing();
        }
        if (selector.RoutePatterns.Distinct(StringComparer.OrdinalIgnoreCase).Count() != selector.RoutePatterns.Count || HasOverlappingMetadata(selector.MetadataTypes))
        {
            throw new CapabilityEndpointSelectorIsAmbiguous();
        }
    }

    /// <summary>Freezes known outside endpoints from the completed startup endpoint graph.</summary>
    /// <param name="sources">The application's configured endpoint data sources.</param>
    /// <exception cref="CapabilityEndpointSelectorMatchesNothing">A selector rule matches no route endpoints.</exception>
    public void Compile(IEnumerable<EndpointDataSource> sources)
    {
        Validate();
        var selector = options.Value.GatedEndpoints;
        var endpoints = sources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().Distinct().ToArray();
        if (selector is null)
        {
            // The whole port is gated; only endpoints that carry the exemption metadata are outside.
            Volatile.Write(ref _outside, endpoints.Where(endpoint => endpoint.Metadata.GetMetadata<CapabilityGateExemption>() is not null).Cast<Endpoint>().ToFrozenSet<Endpoint>(ReferenceEqualityComparer.Instance));
            return;
        }
        if (selector.RoutePatterns.Exists(pattern => !endpoints.Any(endpoint => string.Equals(endpoint.RoutePattern.RawText, pattern, StringComparison.Ordinal))) ||
            selector.MetadataTypes.Exists(type => !endpoints.Any(endpoint => endpoint.Metadata.Any(type.IsInstanceOfType))))
        {
            throw new CapabilityEndpointSelectorMatchesNothing();
        }
        var selected = endpoints.Where(endpoint =>
            selector.RoutePatterns.Contains(endpoint.RoutePattern.RawText, StringComparer.Ordinal) ||
            selector.MetadataTypes.Exists(type => endpoint.Metadata.Any(type.IsInstanceOfType))).ToHashSet();
        Volatile.Write(ref _outside, endpoints.Where(endpoint => !selected.Contains(endpoint)).Cast<Endpoint>().ToFrozenSet<Endpoint>(ReferenceEqualityComparer.Instance));
    }

    /// <summary>Tests positive startup knowledge; unknown endpoints always remain gated.</summary>
    /// <param name="endpoint">The endpoint selected by ASP.NET Core routing.</param>
    /// <returns>Whether this exact endpoint instance was known to be outside at startup.</returns>
    public bool IsOutside(Endpoint? endpoint) => endpoint is not null && Volatile.Read(ref _outside)?.Contains(endpoint) == true;

    static bool HasOverlappingMetadata(List<Type> types)
    {
        for (var first = 0; first < types.Count; first++)
        {
            for (var second = first + 1; second < types.Count; second++)
            {
                if (types[first].IsAssignableFrom(types[second]) || types[second].IsAssignableFrom(types[first]))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
