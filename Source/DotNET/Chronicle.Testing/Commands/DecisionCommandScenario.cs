// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// Owns the single in-process Chronicle store for an opt-in protected command scenario.
/// </summary>
internal sealed class DecisionCommandScenario
{
    readonly IEventLog _eventLog;
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
        : this(store.EventLog, store.UnitOfWorkManager, commandEvents, captureCommands: true)
    {
    }

    /// <summary>
    /// Shares an existing log and transaction manager without changing the scenario's read mode or seeds.
    /// </summary>
    /// <param name="eventLog">The scenario's existing log.</param>
    /// <param name="unitOfWorkManager">The scenario's transaction manager.</param>
    /// <param name="commandEvents">The command append capture.</param>
    /// <param name="captureCommands">Whether to capture commands here rather than through the legacy subscription.</param>
    public DecisionCommandScenario(IEventLog eventLog, IUnitOfWorkManager unitOfWorkManager, List<AppendedEventWithResult> commandEvents, bool captureCommands)
    {
        _eventLog = eventLog;
        EventLog = new EventLogForScenario(eventLog, unitOfWorkManager);
        _commandEvents = commandEvents;
        if (captureCommands)
        {
            eventLog.AppendOperations.Subscribe(events =>
            {
                if (_executionDepth > 0 && !_setupOrCompeting)
                {
                    _commandEvents.AddRange(events.Where(_ => _.Result.IsSuccess && !_excludedSequenceNumbers.Contains(_.Result.SequenceNumber)));
                }
            });
        }
    }

    /// <summary>
    /// Gets the raw sequence shared by seeded facts, competing appends and command commits.
    /// </summary>
    public IEventSequence EventSequence => _eventLog;

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
                    ? await _eventLog.Append(definition, source, @event, route.EventStream, route.EventStreamId)
                    : await _eventLog.Append(source, @event, eventStreamType: route?.EventStreamType, eventStreamId: route?.EventStreamId, eventSourceType: route?.EventSourceType);
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
    public Task AppendCompetingEvents()
    {
        // A nested command shares this scenario but must not drain the outer command's competitor queue.
        if (_executionDepth != 1 || _competitors.Count == 0)
        {
            return Task.CompletedTask;
        }

        // A competing writer must not inherit the owner's read-protection frame or unit of work.
        // A distinct correlation alone does not isolate Chronicle's ambient append refusals under On.
        if (ExecutionContext.IsFlowSuppressed())
        {
            return Task.Run(AppendCompetingEventsOutsideCommand);
        }

        // Flow must be restored on this thread right after scheduling, so the task is started inside and returned after.
        Task competing;
        using (ExecutionContext.SuppressFlow())
        {
            competing = Task.Run(AppendCompetingEventsOutsideCommand);
        }

        return competing;
    }

    async Task AppendCompetingEventsOutsideCommand()
    {
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
                        ? await _eventLog.Append(definition, batch.Source, @event, batch.Route.EventStream, batch.Route.EventStreamId, correlationId: CorrelationId.New())
                        : await _eventLog.Append(batch.Source, @event, eventStreamType: batch.Route?.EventStreamType, eventStreamId: batch.Route?.EventStreamId, eventSourceType: batch.Route?.EventSourceType, correlationId: CorrelationId.New());
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
