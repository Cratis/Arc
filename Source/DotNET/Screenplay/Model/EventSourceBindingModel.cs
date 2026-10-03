// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// The event source definition and stream a command appends through.
/// </summary>
/// <param name="Source">The name of the event source, as the definition declares it.</param>
/// <param name="Stream">The name of the declared stream, or <see langword="null"/> when the command declares none.</param>
/// <param name="StreamDeclared">Whether the definition declares the stream; <see langword="false"/> when the command names a stream it does not.</param>
/// <param name="ConcurrentByStreamId">Whether the stream id takes part in the concurrency scope the definition declares.</param>
public record EventSourceBindingModel(string Source, string? Stream, bool StreamDeclared, bool ConcurrentByStreamId);
