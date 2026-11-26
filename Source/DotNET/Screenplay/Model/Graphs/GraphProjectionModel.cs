// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model.Graphs;

/// <summary>A named projection independently placed from its graph shape.</summary>
/// <param name="Name">The exact projection name.</param>
/// <param name="Namespace">The declaring slice's namespace.</param>
/// <param name="Elements">The owned nodes and edges, including all their handlers.</param>
public record GraphProjectionModel(string Name, string Namespace, IReadOnlyList<GraphElementModel> Elements);
