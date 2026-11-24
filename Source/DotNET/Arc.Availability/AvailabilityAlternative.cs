// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// One way of satisfying a gate: every audience is held, no excluded audience is held and every condition holds.
/// </summary>
/// <remarks>
/// A gate in disjunctive normal form is a list of alternatives, any one of which is enough.
/// </remarks>
/// <param name="Audiences">The audiences that must all be held, in authored order.</param>
/// <param name="Conditions">The 'when' conditions, as authored, that must all hold.</param>
/// <param name="Excluded">The audiences that must not be held ('not'), in authored order.</param>
public record AvailabilityAlternative(IReadOnlyList<AudienceName> Audiences, IReadOnlyList<string> Conditions, IReadOnlyList<AudienceName>? Excluded = null)
{
    /// <summary>
    /// Gets the alternative that holds for everyone.
    /// </summary>
    public static AvailabilityAlternative Unconditional { get; } = new([], []);

    /// <summary>
    /// Gets the audiences that must not be held.
    /// </summary>
    public IReadOnlyList<AudienceName> NotHeld => Excluded ?? [];

    /// <summary>
    /// Combines this alternative with another, so that both must be satisfied.
    /// </summary>
    /// <param name="other">The other alternative.</param>
    /// <returns>The combined alternative.</returns>
    public AvailabilityAlternative And(AvailabilityAlternative other) =>
        new(
            [.. Audiences.Concat(other.Audiences).Distinct()],
            [.. Conditions.Concat(other.Conditions).Distinct()],
            [.. NotHeld.Concat(other.NotHeld).Distinct()]);

    /// <inheritdoc/>
    public virtual bool Equals(AvailabilityAlternative? other) =>
        other is not null && Audiences.SequenceEqual(other.Audiences) && Conditions.SequenceEqual(other.Conditions) && NotHeld.SequenceEqual(other.NotHeld);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        Audiences.ToList().ForEach(_ => hash.Add(_));
        Conditions.ToList().ForEach(_ => hash.Add(_));
        NotHeld.ToList().ForEach(_ => hash.Add(_));
        return hash.ToHashCode();
    }
}
