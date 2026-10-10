// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Owns the single in-process Chronicle store for an opt-in protected command scenario.
/// </summary>
internal sealed class DecisionCommandScenario
{
    readonly Queue<(EventSourceId Source, object[] Events, EventRoute? Route)> _competitors = new();
    readonly List<AppendedEventWithResult> _commandEvents;
    readonly HashSet<EventSequenceNumber> _excludedSequenceNumbers = [];
    bool _setupOrCompeting;
    int _executionDepth;

    /// <summary>
    /// Creates a scenario sharing a real testing store with the command.
    /// </summary>
    /// <param name="store">The shared store.</param>
    /// <param name="commandEvents">The command-only append capture.</param>
    public DecisionCommandScenario(EventStoreForTesting store, List<AppendedEventWithResult> commandEvents)
    {
        Store = store;
        EventLog = new EventLogForScenario(store.EventLog, store.UnitOfWorkManager);
        _commandEvents = commandEvents;
        Store.EventLog.AppendOperations.Subscribe(events =>
        {
            if (_executionDepth > 0 && !_setupOrCompeting)
            {
                _commandEvents.AddRange(events.Where(_ => _.Result.IsSuccess && !_excludedSequenceNumbers.Contains(_.Result.SequenceNumber)));
            }
        });
    }

    /// <summary>
    /// Gets the store used for seeding, decision reads and command commits.
    /// </summary>
    public EventStoreForTesting Store { get; }

    /// <summary>
    /// Gets the log with the store's real unit-of-work manager for transactional appends.
    /// </summary>
    public IEventLog EventLog { get; }

    /// <summary>
    /// Marks a command execution frame, including nested commands.
    /// </summary>
    public void Begin() => _executionDepth++;

    /// <summary>
    /// Ends this command execution frame without ending an enclosing command's capture.
    /// </summary>
    public void End() => _executionDepth--;

    /// <summary>
    /// Schedules a competing append before the owner commits.
    /// </summary>
    /// <param name="source">The competitor's source.</param>
    /// <param name="events">The competing facts.</param>
    /// <param name="route">The competitor's route, if declared.</param>
    /// <exception cref="CompetingEventsMustBeQueuedBeforeExecution">Execution already started.</exception>
    public void QueueCompetingAppend(EventSourceId source, object[] events, EventRoute? route = default)
    {
        if (_executionDepth > 0)
        {
            throw new CompetingEventsMustBeQueuedBeforeExecution();
        }

        _competitors.Enqueue((source, events, route));
    }

    /// <summary>
    /// Seeds prior facts in the same log used by the decision reader.
    /// </summary>
    /// <param name="source">The source of the facts.</param>
    /// <param name="events">The prior facts.</param>
    /// <param name="route">The route to seed, if declared.</param>
    /// <returns>The seed operation.</returns>
    /// <exception cref="EventsMustBeSeededBeforeExecution">Execution already started.</exception>
    /// <exception cref="PriorEventCouldNotBeSeeded">A prior event was not appended.</exception>
    public async Task Seed(EventSourceId source, object[] events, EventRoute? route = default)
    {
        if (_executionDepth > 0)
        {
            throw new EventsMustBeSeededBeforeExecution();
        }

        _setupOrCompeting = true;
        try
        {
            foreach (var @event in events)
            {
                var result = route?.EventSource is { } definition
                    ? await Store.EventLog.Append(definition, source, @event, route.EventStream, route.EventStreamId)
                    : await Store.EventLog.Append(source, @event, eventStreamType: route?.EventStreamType, eventStreamId: route?.EventStreamId, eventSourceType: route?.EventSourceType);
                Exclude(result.SequenceNumber);
                if (!result.IsSuccess)
                {
                    throw new PriorEventCouldNotBeSeeded();
                }
            }
        }
        finally
        {
            _setupOrCompeting = false;
        }
    }

    /// <summary>
    /// Appends queued competing facts after the command reads, outside its unit of work.
    /// </summary>
    /// <returns>The append operation.</returns>
    /// <exception cref="CompetingEventCouldNotBeAppended">A competing append failed.</exception>
    public async Task AppendCompetingEvents()
    {
        // A nested command shares this scenario but must not drain the outer command's competitor queue.
        if (_executionDepth != 1)
        {
            return;
        }

        _setupOrCompeting = true;
        try
        {
            while (_competitors.TryDequeue(out var batch))
            {
                foreach (var @event in batch.Events)
                {
                    // A competitor is not an immediate append by the owner command. Its distinct correlation
                    // keeps the owner's commit observation eligible for operation compensation on conflict.
                    var result = batch.Route?.EventSource is { } definition
                        ? await Store.EventLog.Append(definition, batch.Source, @event, batch.Route.EventStream, batch.Route.EventStreamId, correlationId: CorrelationId.New())
                        : await Store.EventLog.Append(batch.Source, @event, eventStreamType: batch.Route?.EventStreamType, eventStreamId: batch.Route?.EventStreamId, eventSourceType: batch.Route?.EventSourceType, correlationId: CorrelationId.New());
                    Exclude(result.SequenceNumber);
                    if (!result.IsSuccess)
                    {
                        throw new CompetingEventCouldNotBeAppended();
                    }
                }
            }
        }
        finally
        {
            _setupOrCompeting = false;
        }
    }

    void Exclude(EventSequenceNumber sequenceNumber)
    {
        _excludedSequenceNumbers.Add(sequenceNumber);
        _commandEvents.RemoveAll(_ => _.Result.SequenceNumber == sequenceNumber);
    }
}
