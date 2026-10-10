// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>Represents a source-owned command stream.</summary>
/// <param name="Source">The event source name.</param>
/// <param name="Stream">The stream name.</param>
/// <param name="IdentifierType">The command identity's type, if known.</param>
/// <param name="StreamIdType">The dynamic stream id's type, if known.</param>
/// <param name="StreamId">The property supplying the stream id.</param>
public record CommandRouteModel(string Source, string? Stream, TypeReferenceModel? IdentifierType, TypeReferenceModel? StreamIdType, string? StreamId)
{
    /// <summary>
    /// Gets the concrete scalar stream id, when the route uses a literal instead of a command property.
    /// </summary>
    public LiteralSource? StreamIdLiteral { get; init; }

    /// <summary>
    /// Gets the explicitly typed composite parts, in declaration order.
    /// </summary>
    public IReadOnlyList<CommandStreamIdPartModel> StreamIdParts { get; init; } = [];
}
