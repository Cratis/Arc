// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// The place in the model a request runs in: application, then module, feature(s) and slice, outermost first.
/// </summary>
/// <param name="Path">The names from the module down. Empty for the application level.</param>
public record ContextScope(IReadOnlyList<string> Path)
{
    /// <summary>
    /// Gets the application level scope.
    /// </summary>
    public static ContextScope Application { get; } = new([]);

    /// <summary>
    /// Creates a scope from names, outermost first.
    /// </summary>
    /// <param name="path">The names from the module down.</param>
    /// <returns>The scope.</returns>
    public static ContextScope Of(params string[] path) => new(path);

    /// <summary>
    /// Determines whether this scope is the same as, or encloses, another scope.
    /// </summary>
    /// <param name="other">The scope to test.</param>
    /// <returns><see langword="true"/> when <paramref name="other"/> is at or below this scope.</returns>
    public bool Encloses(ContextScope other) =>
        Path.Count <= other.Path.Count && Path.SequenceEqual(other.Path.Take(Path.Count));
}
