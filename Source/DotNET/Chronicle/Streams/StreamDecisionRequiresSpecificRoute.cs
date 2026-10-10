// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Streams;

/// <summary>
/// The exception that is thrown when a stream decision is requested without a specific source and stream route.
/// </summary>
/// <param name="id">The requested event source id.</param>
/// <param name="route">The requested route.</param>
public class StreamDecisionRequiresSpecificRoute(EventSourceId id, EventRoute route)
    : Exception($"A stream decision requires a specified event source id, stream type and stream id; received '{id}' on '{route.EventSourceType}/{route.EventStreamType}/{route.EventStreamId}'.");
