// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootEventHandlers;

#pragma warning disable SA1402, SA1649

[EventType]
public record ThrowingEvent();

public class AggregateRootHandlerFailed() : Exception("The aggregate root handler failed.");

public class AggregateRootWithThrowingHandler : AggregateRoot
{
    public void OnThrowing(ThrowingEvent @event) => throw new AggregateRootHandlerFailed();
}

#pragma warning restore SA1402, SA1649
