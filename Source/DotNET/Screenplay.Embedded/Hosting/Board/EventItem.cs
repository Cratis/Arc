// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents an event on a slice.
/// </summary>
/// <param name="Id">The identity of the event.</param>
/// <param name="Name">The name of the event.</param>
/// <param name="Schema">The JSON Schema of what the event carries.</param>
/// <param name="SourceEventId">The identity of the event this one observes, when the slice consumes an event another slice produces.</param>
/// <param name="Tags">The tags declared on the event.</param>
/// <param name="Constraints">The uniqueness constraints declared for the event.</param>
public record EventItem(
    Guid Id,
    string Name,
    JsonObject Schema,
    string? SourceEventId,
    IReadOnlyList<string> Tags,
    EventConstraints? Constraints);
