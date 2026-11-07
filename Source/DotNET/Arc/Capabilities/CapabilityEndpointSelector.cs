// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities;

/// <summary>
/// Selects the endpoints that require a verified capability caller on the configured port.
/// Pattern and metadata rules are combined as a union.
/// </summary>
public sealed class CapabilityEndpointSelector
{
    /// <summary>Gets the exact route-pattern rules.</summary>
    internal List<string> RoutePatterns { get; } = [];

    /// <summary>Gets the assignable endpoint-metadata rules.</summary>
    internal List<Type> MetadataTypes { get; } = [];

    /// <summary>
    /// Includes endpoints whose route pattern's raw text matches exactly, using ordinal comparison.
    /// This is endpoint configuration text, not a request path, prefix, glob, or regular expression.
    /// </summary>
    /// <param name="pattern">The exact registered route pattern, including any parameters.</param>
    /// <returns>This selector for further configuration.</returns>
    /// <exception cref="ArgumentException">The pattern is null, empty, or whitespace.</exception>
    public CapabilityEndpointSelector ForRoutePattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("A gated endpoint route pattern must not be blank.", nameof(pattern));
        }
        RoutePatterns.Add(pattern);
        return this;
    }

    /// <summary>
    /// Includes endpoints carrying metadata assignable to the given type, including aliases.
    /// </summary>
    /// <typeparam name="TMetadata">The endpoint metadata type.</typeparam>
    /// <returns>This selector for further configuration.</returns>
    public CapabilityEndpointSelector ForEndpointMetadata<TMetadata>()
    {
        MetadataTypes.Add(typeof(TMetadata));
        return this;
    }
}
