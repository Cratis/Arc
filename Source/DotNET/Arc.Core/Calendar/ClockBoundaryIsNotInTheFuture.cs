// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar;

/// <summary>
/// The exception that is thrown when a clock evaluation states a next boundary that is not after the instant it was evaluated at.
/// </summary>
/// <param name="boundary">The boundary the evaluation stated.</param>
/// <param name="evaluatedAt">The instant the evaluation ran at.</param>
/// <remarks>
/// Re-evaluating at such a boundary would state the same boundary again and never advance, so the subscription ends instead.
/// </remarks>
public class ClockBoundaryIsNotInTheFuture(DateTimeOffset boundary, DateTimeOffset evaluatedAt)
    : Exception($"The next boundary {boundary:O} is not after the evaluation instant {evaluatedAt:O}. Return an instant after ClockReading.Now, or null when the result cannot change because of the clock.")
{
    /// <summary>
    /// Gets the boundary the evaluation stated.
    /// </summary>
    public DateTimeOffset Boundary { get; } = boundary;

    /// <summary>
    /// Gets the instant the evaluation ran at.
    /// </summary>
    public DateTimeOffset EvaluatedAt { get; } = evaluatedAt;
}
