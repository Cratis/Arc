// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// A requirement that is a condition: one <c>when</c>.
/// </summary>
/// <param name="Condition">The condition.</param>
public record ConditionRequirementModel(AvailabilityConditionModel Condition) : AvailabilityRequirementModel
{
    /// <inheritdoc/>
    public override IEnumerable<string> Names => [];

    /// <inheritdoc/>
    public override IReadOnlySet<AvailabilityForm> Forms() => new HashSet<AvailabilityForm> { AvailabilityForm.When };
}
