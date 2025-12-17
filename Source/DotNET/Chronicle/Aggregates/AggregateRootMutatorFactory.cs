// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootMutatorFactory"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> to get event sequence to work with.</param>
/// <param name="aggregateRootEventHandlersFactory"><see cref="IAggregateRootEventHandlersFactory"/> for creating event handlers.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing and deserializing events.</param>
/// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/> for correlation id.</param>
public class AggregateRootMutatorFactory(
    IEventStore eventStore,
    IAggregateRootEventHandlersFactory aggregateRootEventHandlersFactory,
    IEventSerializer eventSerializer,
    ICorrelationIdAccessor correlationIdAccessor) : IAggregateRootMutatorFactory
{
    /// <inheritdoc/>
    public Task<IAggregateRootMutator> Create<TAggregateRoot>(IAggregateRootContext context)
    {
        var eventHandlers = aggregateRootEventHandlersFactory.GetFor(context.AggregateRoot);
        var mutator = new AggregateRootMutator(context, eventStore, eventSerializer, eventHandlers, correlationIdAccessor);
        return Task.FromResult<IAggregateRootMutator>(mutator);
    }
}
