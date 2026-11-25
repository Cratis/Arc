// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// The audiences that apply to a request: the union of the tenant's, the user's and the system caller's.
/// </summary>
/// <param name="Matching">The declared audiences held. These, and only these, match a gate.</param>
/// <param name="Undeclared">The grants whose audience the model does not declare. Carried and reported, never matching.</param>
public record EffectiveAudiences(IReadOnlySet<AudienceName> Matching, IReadOnlyCollection<AudienceGrant> Undeclared)
{
    /// <summary>
    /// Gets the audiences held for a request that holds none.
    /// </summary>
    public static EffectiveAudiences None { get; } = new(new HashSet<AudienceName>(), []);

    /// <summary>
    /// Determines whether a declared audience is held.
    /// </summary>
    /// <param name="audience">The audience to check.</param>
    /// <returns><see langword="true"/> if held; otherwise <see langword="false"/>.</returns>
    public bool Holds(AudienceName audience) => Matching.Contains(audience);
}
