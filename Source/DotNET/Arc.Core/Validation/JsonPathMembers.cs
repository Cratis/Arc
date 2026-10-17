// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;

namespace Cratis.Arc.Validation;

/// <summary>
/// Converts JSON failure paths into command validation member names.
/// </summary>
internal static class JsonPathMembers
{
    /// <summary>
    /// Normalizes property casing and bracket notation, retaining array indices.
    /// </summary>
    /// <param name="path">The JSON path.</param>
    /// <returns>The affected member, or no members for a root failure.</returns>
    internal static string[] From(string? path)
    {
        if (string.IsNullOrEmpty(path) || path == "$")
        {
            return [];
        }

        var member = path.Replace("['", ".", StringComparison.Ordinal).Replace("']", string.Empty, StringComparison.Ordinal)
            .Replace("[\"", ".", StringComparison.Ordinal).Replace("\"]", string.Empty, StringComparison.Ordinal)
            .TrimStart('$', '.');

        return [string.Join('.', member.Split('.').Select(segment => segment.ToCamelCase()))];
    }
}
