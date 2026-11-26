// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// One <c>available</c> line of a part: the requirement that has to hold, together with the other lines of the part.
/// </summary>
/// <param name="Requirement">The requirement of the line.</param>
public record AvailabilityGateModel(AvailabilityRequirementModel Requirement)
{
    /// <summary>
    /// Gets the audience and availability names the gate refers to.
    /// </summary>
    public IEnumerable<string> Names => Requirement.Names;

    /// <summary>
    /// Creates a gate over audiences, any one of which is enough.
    /// </summary>
    /// <param name="audiences">The names of the audiences.</param>
    /// <returns>The gate.</returns>
    public static AvailabilityGateModel ForAudiences(IEnumerable<string> audiences) =>
        new(audiences
            .Select(_ => (AvailabilityRequirementModel)new AudienceRequirementModel(_))
            .Aggregate((left, right) => new LogicalRequirementModel(left, false, right)));

    /// <summary>
    /// Creates a gate that builds on a named availability.
    /// </summary>
    /// <param name="availability">The name of the availability.</param>
    /// <returns>The gate.</returns>
    public static AvailabilityGateModel WithAvailability(string availability) => new(new ReferenceRequirementModel(availability));
}
