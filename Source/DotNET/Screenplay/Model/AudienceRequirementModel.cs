// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// A requirement over one audience.
/// </summary>
/// <param name="Audience">The name of the audience.</param>
/// <param name="Negated">Whether the audience must not be held (<c>not</c>).</param>
public record AudienceRequirementModel(string Audience, bool Negated = false) : AvailabilityRequirementModel
{
    /// <inheritdoc/>
    public override IEnumerable<string> Names => [Audience];

    /// <inheritdoc/>
    public override IReadOnlySet<AvailabilityForm> Forms() => new HashSet<AvailabilityForm> { AvailabilityForm.For };
}
