// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the event source definition and stream a reactor or reducer is filtered to with <c>[FromEventSource&lt;TSource&gt;(stream)]</c>.
/// </summary>
/// <param name="Source">The name of the event source definition.</param>
/// <param name="Stream">The name of the stream the observer is filtered to.</param>
/// <param name="StreamDeclared">Whether the event source definition declares the stream.</param>
public record ObservedEventSourceModel(string Source, string? Stream, bool StreamDeclared);
