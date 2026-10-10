// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Streams;

/// <summary>
/// Reads routed stream facts at a captured tail for an explicitly guarded decision.
/// </summary>
public interface IStreamReads
{
    /// <summary>
    /// Reads one stream, capturing its tail before reading its events.
    /// </summary>
    /// <param name="id">The event source id.</param>
    /// <param name="route">The specific stream to read and append to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The decision with its exact concurrency scope.</returns>
    Task<StreamDecision> Stream(EventSourceId id, EventRoute route, CancellationToken ct = default);

    /// <summary>
    /// Reads all streams of a type for a source, guarding the type rather than a single stream id.
    /// </summary>
    /// <param name="id">The event source id.</param>
    /// <param name="route">The stream type to read and the specific stream id to append to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The decision with a stream-type-wide concurrency scope.</returns>
    Task<StreamDecision> StreamType(EventSourceId id, EventRoute route, CancellationToken ct = default);

    /// <summary>
    /// Reads the current command's routed stream.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The decision with its exact concurrency scope.</returns>
    Task<StreamDecision> ForCommand(CancellationToken ct = default);
}
