// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// The resolved routing metadata for an event a command appends.
/// </summary>
/// <param name="EventStreamType">The optional <see cref="EventStreamType"/>.</param>
/// <param name="EventStreamId">The optional <see cref="EventStreamId"/>.</param>
/// <param name="EventSourceType">The optional <see cref="EventSourceType"/>.</param>
/// <param name="Subject">The optional subject.</param>
internal record EventRouting(
    EventStreamType? EventStreamType,
    EventStreamId? EventStreamId,
    EventSourceType? EventSourceType,
    Subject? Subject);
