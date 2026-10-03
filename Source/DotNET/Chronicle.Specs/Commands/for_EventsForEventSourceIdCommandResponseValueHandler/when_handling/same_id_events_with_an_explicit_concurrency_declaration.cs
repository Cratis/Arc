// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

/// <summary>
/// A command that declares concurrency on its attributes made an explicit choice, which keeps its authority.
/// </summary>
public class same_id_events_with_an_explicit_concurrency_declaration : given.an_events_for_event_source_id_command_response_value_handler
{
    IUnitOfWork _unitOfWork;
    Exception _exception;

    void Establish()
    {
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
        _eventLog.Id.Returns(EventSequenceId.Log);
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _commandContext = new(_correlationId, typeof(DeclaringCommand), new DeclaringCommand(), [], new CommandContextValues(), null);
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("command-stream");
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        var id = EventSourceId.New();
        _exception = await Catch.Exception(() => _handler.Handle(
            _commandContext,
            new object[]
            {
                new EventForEventSourceId(id, new TestEvent("first")) { EventStreamId = new EventStreamId("stream-a") },
                new EventForEventSourceId(id, new TestEvent("second")) { EventStreamId = new EventStreamId("stream-b") }
            }));
    }

    void Destroy() => CommandTransaction.Current = null;

    [Fact] void should_not_reject() => _exception.ShouldBeNull();
    [Fact] void should_enroll_both_events() => _unitOfWork.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IUnitOfWork.AddEvent)).ShouldEqual(2);

    [EventStreamId(concurrency: true)]
    public class DeclaringCommand;
}
