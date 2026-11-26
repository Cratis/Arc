// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The exception that is thrown when availability blocks build on each other in a cycle.
/// </summary>
/// <param name="chain">The names in the cycle, ending with the name that closes it.</param>
public class CircularAvailabilityException(IReadOnlyList<string> chain) : Exception($"Availability blocks form a cycle: {string.Join(" -> ", chain)}.")
{
    /// <summary>
    /// Gets the names in the cycle, ending with the name that closes it.
    /// </summary>
    public IReadOnlyList<string> Chain { get; } = chain;
}
