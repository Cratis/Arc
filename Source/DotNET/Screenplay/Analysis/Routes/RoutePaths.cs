// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Joins literal router paths and matches required whole-segment parameters without guessing optional or wildcard paths.
/// </summary>
internal static partial class RoutePaths
{
    /// <summary>
    /// Joins a relative child or verifies the prefix of an absolute child.
    /// </summary>
    /// <param name="parent">The absolute parent pattern.</param>
    /// <param name="child">The literal child pattern.</param>
    /// <returns>The absolute path, or null when unsupported.</returns>
    internal static string? Join(string parent, string child)
    {
        var absolute = child.StartsWith('/') ? child : $"{parent.TrimEnd('/')}/{child}";
        if (!IsPattern(absolute) || (child.StartsWith('/') && parent != "/" &&
            !absolute.StartsWith($"{parent.TrimEnd('/')}/", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(absolute.TrimEnd('/'), parent.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return Normalize(absolute);
    }

    /// <summary>
    /// Gets literal values for an absolute destination matching a route pattern.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <param name="literal">The destination.</param>
    /// <returns>The parameter values, or null when there is no match.</returns>
    internal static IReadOnlyDictionary<string, string>? Match(string pattern, string literal)
    {
        if (!IsDestination(literal))
        {
            return null;
        }

        var expected = Segments(pattern);
        var actual = Segments(literal);
        return expected.Length == actual.Length ? Bind(expected, actual) : null;
    }

    /// <summary>
    /// Tests whether a claim can compete, including unread subtrees and unknown mounts.
    /// </summary>
    /// <param name="entry">The readable or unread route claim.</param>
    /// <param name="literal">The gesture destination.</param>
    /// <returns>Whether the claim cannot be ruled out.</returns>
    internal static bool CouldMatch(RouteEntry entry, string literal)
    {
        if (entry.Path is null)
        {
            return true;
        }

        var expected = Segments(entry.Path);
        var actual = Segments(literal);
        return entry.IncludesDescendants
            ? actual.Length >= expected.Length && Bind(expected, actual[..expected.Length]) is not null
            : Match(entry.Path, literal) is not null;
    }

    static string Normalize(string path) => path.Length > 1 ? path.TrimEnd('/') : path;

    static string[] Segments(string path) => Normalize(path).Split('/', StringSplitOptions.RemoveEmptyEntries);

    static bool IsDestination(string path) => path.StartsWith('/') && !path.Any(_ => _ is '?' or '#' or '*' or '\\' or '%') &&
        !path.Contains("//", StringComparison.Ordinal) && !Segments(path).Any(_ => string.Equals(_, ".", StringComparison.Ordinal) || string.Equals(_, "..", StringComparison.Ordinal));

    static bool IsPattern(string path) => IsDestination(path) && Segments(path).All(IsSegment) &&
        Segments(path).Where(_ => _.StartsWith(':')).Distinct(StringComparer.Ordinal).Count() == Segments(path).Count(_ => _.StartsWith(':'));

    static bool IsSegment(string segment) => segment.StartsWith(':') ? ParameterRegex().IsMatch(segment[1..]) : !segment.Contains(':');

    static Dictionary<string, string>? Bind(string[] expected, string[] actual)
    {
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < expected.Length; index++)
        {
            if (expected[index].StartsWith(':'))
            {
                parameters.Add(expected[index][1..], actual[index]);
            }
            else if (!string.Equals(expected[index], actual[index], StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return parameters;
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ParameterRegex();
}
