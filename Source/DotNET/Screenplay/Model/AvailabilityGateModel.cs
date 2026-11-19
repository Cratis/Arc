// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents one availability gate an artifact carries, written as an Arc attribute.
/// </summary>
/// <param name="Audiences">The audiences of an <c>[AvailableFor]</c> gate, any one of which opens it. Empty for a named availability.</param>
/// <param name="Availability">The name an <c>[AvailableWith]</c> gate refers to, or <see langword="null"/> for an audience gate.</param>
public record AvailabilityGateModel(IEnumerable<string> Audiences, string? Availability = null);
