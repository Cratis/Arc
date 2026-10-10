// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Optional capability of an <see cref="IAggregateRootMutator"/> that exposes the event types its aggregate handles.
/// </summary>
internal interface IAggregateRootHandledEventTypes
{
    /// <summary>
    /// Gets the event types handled during rehydration.
    /// </summary>
    IImmutableList<EventType> EventTypes { get; }
}
