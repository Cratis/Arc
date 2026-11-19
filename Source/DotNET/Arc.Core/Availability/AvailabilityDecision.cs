// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The decision for one part of the application.
/// </summary>
/// <param name="Verdict">The <see cref="AvailabilityVerdict"/>.</param>
/// <param name="Causes">The names of the gate parts that closed it; for diagnostics, never sent to clients.</param>
public record AvailabilityDecision(AvailabilityVerdict Verdict, IReadOnlyList<string> Causes)
{
    /// <summary>
    /// Gets the decision for a part that is available.
    /// </summary>
    public static AvailabilityDecision Available { get; } = new(AvailabilityVerdict.Available, []);

    /// <summary>
    /// Gets a value indicating whether the part is available. Only an available verdict opens a gate.
    /// </summary>
    public bool IsAvailable => Verdict == AvailabilityVerdict.Available;
}
