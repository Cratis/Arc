// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// The result of reading a context value: the value, or its absence.
/// </summary>
/// <remarks>
/// Absence (no key, no matching row, a host value that was not supplied) is a normal outcome and means unknown.
/// It is not a failure; failures are exceptions.
/// </remarks>
/// <param name="IsPresent">Whether a value was found.</param>
/// <param name="Value">The value when present.</param>
public readonly record struct ContextValueOutcome(bool IsPresent, object? Value)
{
    /// <summary>
    /// Gets the outcome for a value that is absent.
    /// </summary>
    public static ContextValueOutcome Absent => default;

    /// <summary>
    /// Creates an outcome for a value. A <see langword="null"/> value is absent.
    /// </summary>
    /// <param name="value">The value found.</param>
    /// <returns>The outcome.</returns>
    public static ContextValueOutcome Present(object? value) => value is null ? Absent : new(true, value);
}
