// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Streams;

/// <summary>
/// Declares that a stream should be completed after a command's events have committed, or as a reactor side effect.
/// </summary>
/// <param name="EventStreamType">The stream type, or null to use the command's route or the observed event's stream type.</param>
/// <param name="EventStreamId">The stream id, or null to use the command's route or the observed event's stream id.</param>
/// <remarks>
/// Completion applies across the event log, not just to one event source. Appending and completing are separate operations.
/// </remarks>
public sealed record CompleteStream(EventStreamType? EventStreamType = default, EventStreamId? EventStreamId = default);
