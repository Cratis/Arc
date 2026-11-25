// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Two requirements joined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The first requirement.</param>
/// <param name="IsAnd">Whether both must hold (<c>and</c>), or either may (<c>or</c>).</param>
/// <param name="Right">The second requirement.</param>
public record LogicalRequirementModel(AvailabilityRequirementModel Left, bool IsAnd, AvailabilityRequirementModel Right) : AvailabilityRequirementModel
{
    /// <inheritdoc/>
    public override IEnumerable<string> Names => [.. Left.Names, .. Right.Names];

    /// <inheritdoc/>
    public override IReadOnlySet<AvailabilityForm> Forms() => new HashSet<AvailabilityForm>(Left.Forms().Concat(Right.Forms()));
}
