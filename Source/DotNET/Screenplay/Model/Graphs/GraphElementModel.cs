// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model.Graphs;

/// <summary>A node label or directed edge type and the events maintaining it.</summary>
/// <param name="Name">The label or edge type.</param>
/// <param name="Source">The source label, or null for a node.</param>
/// <param name="Target">The target label, or null for a node.</param>
/// <param name="Identity">The edge identity property, if any.</param>
/// <param name="Properties">The carried properties, excluding edge endpoints.</param>
/// <param name="Handlers">The event handlers.</param>
public record GraphElementModel(string Name, string? Source, string? Target, string? Identity, IReadOnlyList<GraphPropertyModel> Properties, IReadOnlyList<GraphHandlerModel> Handlers);
