// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// A passively observed predicate, not an enrolled condition or a claim of protection.
/// </summary>
/// <param name="Model">The read model, if known.</param>
/// <param name="Source">The optional source narrowing.</param>
/// <param name="Types">The described event types.</param>
/// <param name="SourceType">The optional source type.</param>
/// <param name="StreamType">The optional stream type.</param>
/// <param name="StreamId">The optional stream identifier.</param>
internal sealed record MeasuredRead(Type? Model, EventSourceId? Source, IReadOnlyList<EventType> Types, EventSourceType? SourceType, EventStreamType? StreamType, EventStreamId? StreamId)
{
    /// <summary>
    /// Tests append notification metadata without evaluating application filters.
    /// </summary>
    /// <param name="context">The actual append metadata.</param>
    /// <returns>Whether the event matches.</returns>
    internal bool Matches(EventContext context) => Matches(context.EventSourceId, context.EventType.Id, context.EventSourceType, context.EventStreamType, context.EventStreamId);

    /// <summary>
    /// Tests scan metadata without fetching or releasing content.
    /// </summary>
    /// <param name="metadata">The scanned metadata.</param>
    /// <returns>Whether the event matches.</returns>
    internal bool Matches(EventMetadata metadata) => Matches(metadata.EventSourceId, metadata.EventTypeId, metadata.EventSourceType, metadata.EventStreamType, metadata.EventStreamId);

    bool Matches(EventSourceId source, EventTypeId type, EventSourceType sourceType, EventStreamType streamType, EventStreamId streamId) =>
        (Source is null || Source == EventSourceId.Unspecified || Source == source) &&
        (Types.Count == 0 || Types.Any(_ => _.Id == type)) &&
        (SourceType is null || SourceType == EventSourceType.Default || SourceType == EventSourceType.Unspecified || SourceType == sourceType) &&
        (StreamType is null || StreamType == EventStreamType.All || StreamType == streamType) &&
        (StreamId is null || StreamId == EventStreamId.Default || StreamId == streamId);
}
