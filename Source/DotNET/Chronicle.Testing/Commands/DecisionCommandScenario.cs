// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>Owns the single in-process Chronicle store for an opt-in protected command scenario.</summary>
internal sealed class DecisionCommandScenario
{
    readonly Queue<(EventSourceId Source, object[] Events)> _competitors = new();
    readonly List<AppendedEventWithResult> _commandEvents;
    readonly HashSet<EventSequenceNumber> _excludedSequenceNumbers = [];
    bool _setupOrCompeting;
    bool _executing;

    /// <summary>Creates a scenario sharing a real testing store with the command.</summary>
    /// <param name="store">The shared store.</param>
    /// <param name="commandEvents">The command-only append capture.</param>
    public DecisionCommandScenario(EventStoreForTesting store, List<AppendedEventWithResult> commandEvents)
    {
        Store = store;
        _commandEvents = commandEvents;
        Store.EventLog.AppendOperations.Subscribe(events =>
        {
            if (_executing && !_setupOrCompeting)
                _commandEvents.AddRange(events.Where(_ => _.Result.IsSuccess && !_excludedSequenceNumbers.Contains(_.Result.SequenceNumber)));
        });
    }

    /// <summary>Gets the store used for seeding, decision reads and command commits.</summary>
    public EventStoreForTesting Store { get; }

    /// <summary>Marks the command execution window.</summary>
    public void Begin() => _executing = true;

    /// <summary>Ends the command execution window.</summary>
    public void End() => _executing = false;

    /// <summary>Schedules a competing append before the owner commits.</summary>
    /// <param name="source">The competitor's source.</param>
    /// <param name="events">The competing facts.</param>
    /// <exception cref="InvalidOperationException">Execution already started.</exception>
    public void QueueCompetingAppend(EventSourceId source, object[] events)
    {
        if (_executing) throw new InvalidOperationException("Queue competing events before executing the command.");
        _competitors.Enqueue((source, events));
    }

    /// <summary>Seeds prior facts in the same log used by the decision reader.</summary>
    /// <param name="source">The source of the facts.</param>
    /// <param name="events">The prior facts.</param>
    /// <returns>The seed operation.</returns>
    /// <exception cref="InvalidOperationException">Execution already started.</exception>
    public async Task Seed(EventSourceId source, object[] events)
    {
        if (_executing) throw new InvalidOperationException("Seed events before executing the command.");
        _setupOrCompeting = true;
        try
        {
            foreach (var @event in events)
            {
                var result = await Store.EventLog.Append(source, @event);
                Exclude(result.SequenceNumber);
                if (!result.IsSuccess) throw new InvalidOperationException("A prior event could not be seeded.");
            }
        }
        finally
        {
            _setupOrCompeting = false;
        }
    }

    /// <summary>Appends queued competing facts after the command reads, outside its unit of work.</summary>
    /// <returns>The append operation.</returns>
    /// <exception cref="InvalidOperationException">A competing append failed.</exception>
    public async Task AppendCompetingEvents()
    {
        _setupOrCompeting = true;
        try
        {
            while (_competitors.TryDequeue(out var batch))
            {
                foreach (var @event in batch.Events)
                {
                    var result = await Store.EventLog.Append(batch.Source, @event);
                    Exclude(result.SequenceNumber);
                    if (!result.IsSuccess) throw new InvalidOperationException("A competing event could not be appended.");
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
