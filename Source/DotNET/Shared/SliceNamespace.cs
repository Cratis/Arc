// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Slices;

/// <summary>
/// The namespace mapping shared by the proxy generator and the runtime: it decides which namespace segments
/// locate a command or query, both for the folder its proxy is generated to and for its slice location.
/// </summary>
/// <remarks>
/// A matching namespace root wins over segment skipping. When several roots match, the longest one is used.
/// </remarks>
internal static class SliceNamespace
{
    /// <summary>
    /// The namespace prefix removed before skipping segments, so it never becomes part of a folder or a slice location.
    /// </summary>
    internal const string ApiNamespacePrefix = "Cratis.API";

    /// <summary>
    /// Removes the <see cref="ApiNamespacePrefix"/> from a namespace.
    /// </summary>
    /// <param name="ns">The namespace to remove from.</param>
    /// <returns>The namespace without the prefix.</returns>
    internal static string StripApiNamespacePrefix(string ns) => ns.Replace(ApiNamespacePrefix, string.Empty, StringComparison.Ordinal);

    /// <summary>
    /// Finds the longest namespace root that the namespace equals or is nested under.
    /// </summary>
    /// <param name="ns">The namespace to find a root for.</param>
    /// <param name="roots">The configured namespace roots.</param>
    /// <returns>The matching root, or <see langword="null"/> when none matches.</returns>
    internal static string? FindRoot(string ns, IEnumerable<string> roots) =>
        roots
            .Where(root => ns == root || ns.StartsWith(root + ".", StringComparison.Ordinal))
            .OrderByDescending(root => root.Length)
            .FirstOrDefault();

    /// <summary>
    /// Gets the namespace segments below a namespace root.
    /// </summary>
    /// <param name="ns">The namespace, which must equal or be nested under <paramref name="root"/>.</param>
    /// <param name="root">The namespace root.</param>
    /// <returns>The segments below the root; empty when the namespace is the root itself.</returns>
    internal static string[] SegmentsBelowRoot(string ns, string root) =>
        ns == root ? [] : ns[(root.Length + 1)..].Split('.');

    /// <summary>
    /// Gets the namespace segments that remain after skipping a number of leading segments.
    /// </summary>
    /// <param name="ns">The namespace.</param>
    /// <param name="segmentsToSkip">The number of leading segments to skip.</param>
    /// <returns>The remaining segments.</returns>
    internal static string[] SegmentsAfterSkipping(string ns, int segmentsToSkip) =>
        [.. ns.Split('.').Skip(segmentsToSkip)];

    /// <summary>
    /// Resolves the namespace segments that locate a type: below the matching root, or after removing the
    /// <see cref="ApiNamespacePrefix"/> and skipping segments. This is the same mapping that decides the folder a proxy is
    /// generated to, so the folder and the slice location never differ.
    /// </summary>
    /// <param name="ns">The namespace of the type.</param>
    /// <param name="roots">The configured namespace roots.</param>
    /// <param name="segmentsToSkip">The number of leading segments to skip when no root matches.</param>
    /// <returns>The segments that locate the type.</returns>
    internal static string[] Resolve(string ns, IEnumerable<string> roots, int segmentsToSkip) =>
        FindRoot(ns, roots) is { } root ? SegmentsBelowRoot(ns, root) : SegmentsAfterSkipping(StripApiNamespacePrefix(ns), segmentsToSkip);

    /// <summary>
    /// Splits the segments that locate a type into module, feature and slice.
    /// </summary>
    /// <param name="segments">The segments that locate the type.</param>
    /// <returns>
    /// The first segment as module, the last as slice and those between as feature, joined with a period.
    /// When there are fewer than three segments, the deepest parts are filled and the rest are empty.
    /// </returns>
    internal static (string Module, string Feature, string Slice) ToParts(IEnumerable<string> segments)
    {
        var parts = segments.Where(segment => !string.IsNullOrEmpty(segment)).ToArray();
        return parts.Length switch
        {
            0 => (string.Empty, string.Empty, string.Empty),
            1 => (string.Empty, string.Empty, parts[0]),
            2 => (string.Empty, parts[0], parts[1]),
            _ => (parts[0], string.Join('.', parts[1..^1]), parts[^1])
        };
    }
}
