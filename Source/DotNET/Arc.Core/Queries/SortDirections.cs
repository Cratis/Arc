// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Parses the sort direction of a query request, for every transport that carries one.
/// </summary>
/// <remarks>
/// Both the short and long spelling of each direction are accepted, case-insensitively, and anything else is
/// rejected. Falling through to ascending would answer the request in the opposite order to the one asked for,
/// without saying so. The accepted set matches Arc for Kotlin and Java, so one documented wire contract describes
/// both implementations.
/// </remarks>
internal static class SortDirections
{
    /// <summary>
    /// Parses a sort direction.
    /// </summary>
    /// <param name="value">The value as it arrived on the wire, or null when the request omitted it.</param>
    /// <param name="member">The name of the request member the direction was read from, for the rejection.</param>
    /// <returns>The parsed <see cref="SortDirection"/>.</returns>
    /// <exception cref="SortDirectionIsNotRecognized">Thrown when the value is not a recognized direction.</exception>
    public static SortDirection Parse(string? value, string member)
    {
        if (value is null)
        {
            return SortDirection.Ascending;
        }

        if (value.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("ascending", StringComparison.OrdinalIgnoreCase))
        {
            return SortDirection.Ascending;
        }

        if (value.Equals("desc", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("descending", StringComparison.OrdinalIgnoreCase))
        {
            return SortDirection.Descending;
        }

        throw new SortDirectionIsNotRecognized(value, member);
    }
}
