// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Contexts;

/// <summary>
/// The outcome of reading a request-scoped context value.
/// </summary>
/// <param name="IsPresent">Whether the value is present. An absent value is unknown, never a denial.</param>
/// <param name="Value">The value, or <see langword="null"/> when it is absent.</param>
public readonly record struct ContextValueRead(bool IsPresent, object? Value)
{
    /// <summary>
    /// Gets the outcome for a value that is absent: no key, no matching row, or a value the host did not supply.
    /// </summary>
    public static ContextValueRead Absent => default;

    /// <summary>
    /// Creates the outcome for a value that is present.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="ContextValueRead"/>; absent when <paramref name="value"/> is <see langword="null"/>.</returns>
    public static ContextValueRead Present(object? value) => value is null ? Absent : new(true, value);
}
