// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a block observing one or more event types.
/// </summary>
/// <param name="EventTypes">The identifiers of the event types observed.</param>
/// <param name="Key">The key expression, if the block declares one.</param>
/// <param name="ParentKey">The parent key expression, if the block declares one.</param>
/// <param name="Properties">The read model property to event expression map.</param>
public record ProjectionFromModel(
    IEnumerable<string> EventTypes,
    string? Key,
    string? ParentKey,
    IReadOnlyDictionary<string, string> Properties);
