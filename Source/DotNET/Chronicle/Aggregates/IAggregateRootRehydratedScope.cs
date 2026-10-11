// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Optional capability of an <see cref="IAggregateRootMutator"/> that exposes the event types its rehydrated scope guards.
/// </summary>
internal interface IAggregateRootRehydratedScope
{
    /// <summary>
    /// Gets the handled event types when rehydration found an existing aggregate; otherwise null to guard all types.
    /// </summary>
    IImmutableList<EventType>? GuardedEventTypes { get; }
}
