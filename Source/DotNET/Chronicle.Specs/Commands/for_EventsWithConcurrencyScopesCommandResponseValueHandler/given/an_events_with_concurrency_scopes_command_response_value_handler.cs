// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.given;

public class an_events_with_concurrency_scopes_command_response_value_handler : Specification
{
    protected EventsWithConcurrencyScopesCommandResponseValueHandler _handler;
    protected IEventLog _eventLog;
    protected RecordingUnitOfWork _unitOfWork;
    protected CommandContext _commandContext;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _eventLog = Substitute.For<IEventLog>();
        _eventLog.Id.Returns(EventSequenceId.Log);
        _unitOfWork = new();
        _handler = new(_eventLog);
        _commandContext = new(
            _correlationId,
            typeof(TestCommand),
            new TestCommand(),
            [],
            new CommandContextValues(),
            null);
    }

    void Destroy() => CommandTransaction.Current = null;

    public class TestCommand;
    public record FirstEvent(string Value);
    public record SecondEvent(int Value);

    protected sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public bool IsCompleted => false;
        public CorrelationId CorrelationId => CorrelationId.NotSet;
        public bool IsSuccess => true;
        public int AddEventsCallCount { get; private set; }
        public EventSequenceId AddedEventSequenceId { get; private set; }
        public IReadOnlyList<EventForEventSourceId> AddedEvents { get; private set; } = [];
        public IReadOnlyList<KeyValuePair<EventSourceId, ConcurrencyScope>> AddedConcurrencyScopes { get; private set; } = [];

        public void AddEvent(
            EventSequenceId eventSequenceId,
            EventSourceId eventSourceId,
            object @event,
            Causation causation,
            EventStreamType? eventStreamType = default,
            EventStreamId? eventStreamId = default,
            EventSourceType? eventSourceType = default,
            ConcurrencyScope? concurrencyScope = default,
            IEnumerable<string>? tags = default,
            DateTimeOffset? occurred = default,
            Subject? subject = default)
        {
        }

        public void AddEvents(
            EventSequenceId eventSequenceId,
            IEnumerable<EventForEventSourceId> events,
            IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes)
        {
            AddEventsCallCount++;
            AddedEventSequenceId = eventSequenceId;
            AddedEvents = events.ToArray();
            AddedConcurrencyScopes = concurrencyScopes.ToArray();
        }

        public IEnumerable<object> GetEvents() => [];
        public IEnumerable<ConstraintViolation> GetConstraintViolations() => [];
        public IEnumerable<ConcurrencyViolation> GetConcurrencyViolations() => [];
        public IEnumerable<AppendError> GetAppendErrors() => [];
        public Task Commit() => Task.CompletedTask;
        public Task Rollback() => Task.CompletedTask;
        public void OnCompleted(Action<IUnitOfWork> callback)
        {
        }

        public bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber)
        {
            eventSequenceNumber = null;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
