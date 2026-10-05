// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Where a derived <see cref="Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScope"/> came from.
/// </summary>
internal enum DerivedConcurrencyScopeOrigin
{
    /// <summary>
    /// The command declares no guard for the event.
    /// </summary>
    None = 0,

    /// <summary>
    /// The command declared concurrency on its metadata attributes; the caller chose it.
    /// </summary>
    Explicit = 1,

    /// <summary>
    /// The guard follows from the command's event source definition, built from the event's routing.
    /// </summary>
    Implicit = 2,

    /// <summary>
    /// The event is written through a definition and the event sequence derives its guard from that definition.
    /// </summary>
    DerivedByEventSequence = 3
}
