// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// A requirement that builds on a named availability.
/// </summary>
/// <param name="Availability">The name of the availability.</param>
public record ReferenceRequirementModel(string Availability) : AvailabilityRequirementModel
{
    /// <inheritdoc/>
    public override IEnumerable<string> Names => [Availability];

    /// <inheritdoc/>
    public override IReadOnlySet<AvailabilityForm> Forms() => new HashSet<AvailabilityForm> { AvailabilityForm.With };
}
