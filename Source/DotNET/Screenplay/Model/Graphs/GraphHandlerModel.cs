// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model.Graphs;

/// <summary>An event's graph mutation, using Chronicle's projection expression notation.</summary>
/// <param name="Event">The event name.</param>
/// <param name="Namespace">The event's declaring namespace.</param>
/// <param name="Removes">Whether the element is removed.</param>
/// <param name="Detach">Whether attached edges are removed with a node.</param>
/// <param name="Key">The node key expression.</param>
/// <param name="Source">The edge source expression.</param>
/// <param name="Target">The edge target expression.</param>
/// <param name="Identity">The identifying edge expression.</param>
/// <param name="Mappings">The property mappings.</param>
public record GraphHandlerModel(string Event, string Namespace, bool Removes, bool Detach, string? Key, string? Source, string? Target, string? Identity, IReadOnlyDictionary<string, string> Mappings);
