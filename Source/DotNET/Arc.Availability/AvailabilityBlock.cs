// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Represents a reusable availability definition.
/// </summary>
/// <param name="Name">The name of the definition.</param>
/// <param name="Description">What it stands for, or empty.</param>
/// <param name="Source">Where it is declared, or empty.</param>
/// <param name="Alternatives">The alternatives, any one of which satisfies the definition.</param>
public record AvailabilityBlock(string Name, string Description, string Source, IReadOnlyList<AvailabilityAlternativeModel> Alternatives);
