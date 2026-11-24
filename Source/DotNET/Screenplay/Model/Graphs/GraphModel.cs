// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model.Graphs;

/// <summary>
/// A property graph recovered from Chronicle node and edge annotations.
/// </summary>
/// <param name="Name">The graph name.</param>
/// <param name="Namespace">The namespace declaring the graph's canonical projection, or its shape when unobserved.</param>
/// <param name="Elements">The nodes and edges.</param>
/// <remarks>
/// One projection per graph, placed with its ordinal-first declaring event slice and named after that slice,
/// is an Arc fallback. Chronicle's annotations do not retain Stage projection names or slice ownership.
/// </remarks>
public record GraphModel(string Name, string Namespace, IReadOnlyList<GraphElementModel> Elements)
{
    /// <summary>Gets whether a source-declared event slice supports canonical projection placement.</summary>
    public bool HasProjectionPlacement { get; init; } = true;
}
