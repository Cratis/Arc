// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents an event a specification states, as given or as expected.
/// </summary>
/// <param name="Id">The identity of the step.</param>
/// <param name="Name">The name of the event, with its route when the specification states one.</param>
/// <param name="EventId">The identity of the event item declaring the event, or <see cref="Guid.Empty"/> when nothing declares it.</param>
/// <param name="Values">The values the specification states for the event's properties.</param>
public record SpecificationStep(Guid Id, string Name, Guid EventId, JsonObject Values);
