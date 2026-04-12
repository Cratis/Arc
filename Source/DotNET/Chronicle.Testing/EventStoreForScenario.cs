// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Arc.Chronicle.Testing;

/// <summary>
/// Represents an <see cref="IEventStore"/> backed by an in-memory <see cref="EventScenario"/>.
/// </summary>
/// <param name="eventScenario">The in-memory <see cref="EventScenario"/> to route operations through.</param>
internal sealed class EventStoreForScenario(EventScenario eventScenario) : IEventStore
{
    /// <inheritdoc/>
    public EventStoreName Name => "test-event-store";

    /// <inheritdoc/>
    public EventStoreNamespaceName Namespace => "default";

    /// <inheritdoc/>
    public IChronicleConnection Connection =>
        throw new NotSupportedException("Connection is not supported for command scenarios.");

    /// <inheritdoc/>
    public IUnitOfWorkManager UnitOfWorkManager =>
        throw new NotSupportedException("Unit of work manager is not supported for command scenarios.");

    /// <inheritdoc/>
    public IEventTypes EventTypes => Defaults.Instance.EventTypes;

    /// <inheritdoc/>
    public IConstraints Constraints =>
        throw new NotSupportedException("Constraints are not exposed through command scenarios.");

    /// <inheritdoc/>
    public IEventLog EventLog => eventScenario.EventLog;

    /// <inheritdoc/>
    public IJobs Jobs =>
        throw new NotSupportedException("Jobs are not supported for command scenarios.");

    /// <inheritdoc/>
    public IReactors Reactors =>
        throw new NotSupportedException("Reactors are not supported for command scenarios.");

    /// <inheritdoc/>
    public IReducers Reducers =>
        throw new NotSupportedException("Reducers are not supported for command scenarios.");

    /// <inheritdoc/>
    public IProjections Projections =>
        throw new NotSupportedException("Projections are not supported for command scenarios.");

    /// <inheritdoc/>
    public IWebhooks Webhooks =>
        throw new NotSupportedException("Webhooks are not supported for command scenarios.");

    /// <inheritdoc/>
    public IEventStoreSubscriptions Subscriptions =>
        throw new NotSupportedException("Subscriptions are not supported for command scenarios.");

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions =>
        throw new NotSupportedException("Failed partitions are not supported for command scenarios.");

    /// <inheritdoc/>
    public IReadModels ReadModels =>
        throw new NotSupportedException("Read models are not supported for command scenarios.");

    /// <inheritdoc/>
    public IEventSeeding Seeding =>
        throw new NotSupportedException("Seeding is not supported for command scenarios.");

    /// <inheritdoc/>
    public Task DiscoverAll() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task RegisterAll() => Task.CompletedTask;

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) => eventScenario.EventSequence;

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<EventStoreNamespaceName>>([Namespace]);
}
