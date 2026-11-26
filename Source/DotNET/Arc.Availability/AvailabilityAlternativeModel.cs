// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents one way of satisfying a gate: all its audiences and conditions must hold.
/// </summary>
/// <param name="Audiences">The audiences that must all hold.</param>
/// <param name="Conditions">The conditions that must all hold, as written.</param>
public record AvailabilityAlternativeModel(IReadOnlyList<string> Audiences, IReadOnlyList<string> Conditions);
