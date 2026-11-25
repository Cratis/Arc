// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents a gate guarding a part of the application.
/// </summary>
/// <param name="Target">The part the gate guards.</param>
/// <param name="Alternatives">The alternatives, any one of which opens the gate.</param>
public record AvailabilityGate(string Target, IReadOnlyList<AvailabilityAlternativeModel> Alternatives);
