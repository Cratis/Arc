// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AggregateRootFactory"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> to get event sequence to work with.</param>
/// <param name="mutatorFactory"><see cref="IAggregateRootMutatorFactory"/> for creating mutators.</param>
/// <param name="unitOfWorkManager"><see cref="IUnitOfWorkManager"/> for managing units of work.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
public class AggregateRootFactory(
    IEventStore eventStore,
    IAggregateRootMutatorFactory mutatorFactory,
    IUnitOfWorkManager unitOfWorkManager,
    IServiceProvider serviceProvider) : IAggregateRootFactory
{
    /// <inheritdoc/>
    public async Task<TAggregateRoot> Get<TAggregateRoot>(EventSourceId id, EventStreamId? streamId = default, EventSourceType? eventSourceType = default)
        where TAggregateRoot : IAggregateRoot
    {
        // TODO: Create Issue: Must dispose of unit of work in some way or else it's a memory leak.
        var unitOfWork = unitOfWorkManager.HasCurrent ? unitOfWorkManager.Current : unitOfWorkManager.Begin(CorrelationId.New());

        var aggregateRoot = ActivatorUtilities.CreateInstance<TAggregateRoot>(serviceProvider);
        var eventSequence = eventStore.GetEventSequence(EventSequenceId.Log);
        var eventStreamType = aggregateRoot.GetEventStreamType();
        streamId ??= EventStreamId.Default;
        var routing = AggregateRootEventSourceRouting.Resolve(typeof(TAggregateRoot), serviceProvider.GetRequiredService<IEventSources>, eventSourceType);
        if (routing is not null)
        {
            eventSourceType = routing.EventSourceType;
            eventStreamType = routing.EventStreamType;
        }

        eventSourceType ??= EventSourceType.Default;

        var context = new AggregateRootContext(
            eventSourceType,
            id,
            eventStreamType,
            streamId,
            eventSequence,
            aggregateRoot,
            unitOfWork,
            EventSequenceNumber.First,

            // A new aggregate has no event in its scope, so rehydration leaves this as it is. Expecting
            // BeforeFirst makes the commit mean "no event may exist in this scope yet", where expecting the first
            // sequence number would accept a concurrent writer's own first event, which sits at that very number.
            EventSequenceNumber.BeforeFirst,
            routing?.EventSource,
            routing?.EventStream);

        var mutator = await mutatorFactory.Create<TAggregateRoot>(context);
        await mutator.Rehydrate();

        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            knownAggregateRoot._mutation = new AggregateRootMutation(context, mutator, eventSequence);
            knownAggregateRoot._context = context;
            await knownAggregateRoot.InternalOnActivate();
        }

        return aggregateRoot;
    }
}
