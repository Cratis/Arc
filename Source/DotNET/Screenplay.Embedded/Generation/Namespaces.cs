// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Reads namespaces the way the document hierarchy is resolved from them.
/// </summary>
/// <remarks>
/// Every comparison here is on whole segments. A namespace is not within another because its text starts with the
/// same characters - <c>Library.Authorship</c> is not inside <c>Library.Author</c> - and a prefix stripped on
/// characters rather than on segments would quietly reparent half an application.
/// </remarks>
public static class Namespaces
{
    /// <summary>
    /// The character separating the segments of a namespace.
    /// </summary>
    public const char Separator = '.';

    /// <summary>
    /// Splits a namespace into its segments.
    /// </summary>
    /// <param name="namespace">The namespace to split.</param>
    /// <returns>The segments, outermost first.</returns>
    public static IReadOnlyList<string> Segments(string? @namespace) =>
        string.IsNullOrWhiteSpace(@namespace)
            ? []
            : [.. @namespace.Split(Separator, StringSplitOptions.RemoveEmptyEntries)];

    /// <summary>
    /// Joins segments into a namespace.
    /// </summary>
    /// <param name="segments">The segments to join.</param>
    /// <returns>The namespace.</returns>
    public static string Join(IEnumerable<string> segments) => string.Join(Separator, segments);

    /// <summary>
    /// Gets a value indicating whether a namespace is the scope itself or sits beneath it.
    /// </summary>
    /// <param name="namespace">The namespace to place.</param>
    /// <param name="scope">The namespace of the scope.</param>
    /// <returns>True when the namespace is within the scope, false otherwise.</returns>
    public static bool IsWithin(string? @namespace, string scope)
    {
        if (string.IsNullOrEmpty(scope))
        {
            return true;
        }

        if (string.IsNullOrEmpty(@namespace))
        {
            return false;
        }

        return string.Equals(@namespace, scope, StringComparison.Ordinal) ||
            (@namespace.Length > scope.Length &&
                @namespace.StartsWith(scope, StringComparison.Ordinal) &&
                @namespace[scope.Length] == Separator);
    }

    /// <summary>
    /// Gets the segments of a namespace that remain once the segments of a root namespace are stripped from it.
    /// </summary>
    /// <param name="namespace">The namespace to strip.</param>
    /// <param name="root">The root namespace to strip.</param>
    /// <returns>The remaining segments, or <see langword="null"/> when the namespace is not within the root.</returns>
    public static IReadOnlyList<string>? Relative(string? @namespace, string root)
    {
        if (!IsWithin(@namespace, root))
        {
            return null;
        }

        var segments = Segments(@namespace);

        return [.. segments.Skip(Segments(root).Count)];
    }

    /// <summary>
    /// Gets the last segment of a namespace.
    /// </summary>
    /// <param name="namespace">The namespace to read.</param>
    /// <param name="fallback">The name to answer with when the namespace holds no segment.</param>
    /// <returns>The last segment.</returns>
    public static string LastSegment(string? @namespace, string fallback)
    {
        var segments = Segments(@namespace);

        return segments.Count == 0 ? fallback : segments[^1];
    }
}
