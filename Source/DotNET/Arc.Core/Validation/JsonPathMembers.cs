// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;

namespace Cratis.Arc.Validation;

/// <summary>
/// Converts JSON failure paths and validation member paths into camelCase field paths.
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

        return [CamelCaseSegments(member.Split('.'))];
    }

    /// <summary>
    /// Normalizes field paths while preserving empty members and non-path identifiers.
    /// </summary>
    /// <param name="member">The member reported by a validation rule.</param>
    /// <returns>The camelCase field path, or the original member when it is not a field path.</returns>
    internal static string Normalize(string member)
    {
        if (string.IsNullOrEmpty(member))
        {
            return member;
        }

        var segments = member.Split('.');

        return segments.All(IsFieldSegment) ? CamelCaseSegments(segments) : member;
    }

    /// <summary>
    /// Normalizes and materializes validation members, treating a missing collection as empty.
    /// </summary>
    /// <param name="members">The members reported by validation rules.</param>
    /// <returns>The normalized members.</returns>
    internal static string[] NormalizeMembers(IEnumerable<string>? members) =>
        (members ?? []).Select(Normalize).ToArray();

    static string CamelCaseSegments(IEnumerable<string> segments) =>
        string.Join('.', segments.Select(segment => segment.ToCamelCase()));

    static bool IsFieldSegment(string segment)
    {
        if (segment.Length == 0 || !(char.IsLetter(segment[0]) || segment[0] == '_'))
        {
            return false;
        }

        var offset = SkipIdentifierCharacters(segment, 1);
        while (offset < segment.Length)
        {
            if (!TryReadIndex(segment, ref offset))
            {
                return false;
            }
        }

        return true;
    }

    static int SkipIdentifierCharacters(string segment, int offset)
    {
        while (offset < segment.Length && (char.IsLetterOrDigit(segment[offset]) || segment[offset] == '_'))
        {
            offset++;
        }

        return offset;
    }

    static bool TryReadIndex(string segment, ref int offset)
    {
        if (segment[offset++] != '[')
        {
            return false;
        }

        var firstDigit = offset;
        while (offset < segment.Length && char.IsAsciiDigit(segment[offset]))
        {
            offset++;
        }

        return offset != firstDigit && offset != segment.Length && segment[offset++] == ']';
    }
}
