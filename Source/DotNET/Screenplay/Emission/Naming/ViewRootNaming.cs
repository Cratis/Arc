// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Emission.Naming;

/// <summary>
/// Names the paths of one command, writing a path through a read the document names by its view as that view is named.
/// </summary>
/// <remarks>
/// Screenplay names a read by its alias, or by its read model when it states no alias, and resolves a read path from
/// that name. A path is otherwise a property path whose segments start in lower case, which a read named
/// <c>Current</c> does not resolve from.
/// </remarks>
/// <param name="inner">The naming of the document.</param>
/// <param name="roots">The read names by the property name the paths of the model start with.</param>
internal sealed class ViewRootNaming(IScreenplayNaming inner, IReadOnlyDictionary<string, string> roots) : IScreenplayNaming
{
    /// <inheritdoc/>
    public string ToPropertyName(string name) => inner.ToPropertyName(name);

    /// <inheritdoc/>
    public string ToPropertyPath(string path) =>
        path.IndexOf('.', StringComparison.Ordinal) is var dot && dot > 0 && roots.TryGetValue(inner.ToPropertyName(path[..dot]), out var root) && path.Length > dot + 1
            ? $"{root}.{inner.ToPropertyPath(path[(dot + 1)..])}"
            : inner.ToPropertyPath(path);

    /// <inheritdoc/>
    public string ToDeclarationName(string name) => inner.ToDeclarationName(name);

    /// <inheritdoc/>
    public string? ToStringLiteral(string? value) => inner.ToStringLiteral(value);

    /// <inheritdoc/>
    public string? ToFilePath(string? path) => inner.ToFilePath(path);
}
