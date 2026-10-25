// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.given;

/// <summary>
/// Gives Castle an abstract invocation target for Chronicle's default <see cref="IEventSequence.SupportsIdempotentAppends"/>.
/// </summary>
public abstract class IdempotentEventLog : EventLogWithNamedTags, IEventLog
{
    public abstract bool SupportsIdempotentAppends { get; }
}
