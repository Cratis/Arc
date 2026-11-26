// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The result of evaluating availability for a request.
/// </summary>
/// <remarks>
/// A failure (store unavailable, code throws) is never a result: it is an exception, so an outage is not shown as "not available".
/// </remarks>
/// <param name="Outcome">The outcome.</param>
/// <param name="Causes">For a result that is not available, the parts of the gate that caused it, as authored, for example 'for Plus'.</param>
public record AvailabilityResult(AvailabilityOutcome Outcome, IReadOnlyList<string> Causes)
{
    /// <summary>
    /// Gets the result for something that is available.
    /// </summary>
    public static AvailabilityResult Available { get; } = new(AvailabilityOutcome.Available, []);

    /// <summary>
    /// Gets a value indicating whether the gate is open. Unknown does not open it.
    /// </summary>
    public bool IsAvailable => Outcome == AvailabilityOutcome.Available;

    /// <summary>
    /// Gets the availability reason when not available, or <see langword="null"/> when available.
    /// </summary>
    public string? Reason => IsAvailable ? null : AvailabilityReasons.NotAvailable;
}
