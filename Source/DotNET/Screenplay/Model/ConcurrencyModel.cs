// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the scope a command's appends are checked for concurrent writers within.
/// </summary>
/// <param name="EventSource">Whether the scope includes the event source.</param>
/// <param name="SourceType">The event source type the scope is narrowed to, if any.</param>
/// <param name="StreamType">The event stream type the scope is narrowed to, if any.</param>
/// <param name="StreamId">The event stream identifier the scope is narrowed to, if any.</param>
/// <param name="EventTypes">The event types the scope is narrowed to.</param>
/// <remarks>
/// A concurrency block that narrows nothing at all is a compile error, so a model carrying no dimension is left out
/// of the document rather than emitted empty.
/// </remarks>
public record ConcurrencyModel(
    bool EventSource,
    string? SourceType,
    string? StreamType,
    string? StreamId,
    IEnumerable<string> EventTypes);
