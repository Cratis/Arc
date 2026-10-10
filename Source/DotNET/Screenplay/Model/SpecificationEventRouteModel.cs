// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the concrete route of one event occurrence, separately from its payload and source identity.
/// </summary>
/// <param name="Source">The declared event source, or null for an unrouted occurrence.</param>
/// <param name="Stream">The declared stream, or null for an unrouted occurrence.</param>
/// <param name="StreamId">The concrete scalar stream id, when one was stated.</param>
public record SpecificationEventRouteModel(string? Source, string? Stream, LiteralSource? StreamId)
{
    /// <summary>
    /// Gets the route of an occurrence appended without source or stream metadata.
    /// </summary>
    public static readonly SpecificationEventRouteModel NoStream = new(null, null, null);

    /// <summary>
    /// Gets the concrete composite stream-id parts, in declaration order.
    /// </summary>
    public IReadOnlyList<PropertyMappingModel> StreamIdParts { get; init; } = [];
}
