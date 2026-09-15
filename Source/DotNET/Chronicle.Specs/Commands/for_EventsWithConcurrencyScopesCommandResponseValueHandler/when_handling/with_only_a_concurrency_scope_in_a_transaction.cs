// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_only_a_concurrency_scope_in_a_transaction : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    EventSourceId _label;
    ConcurrencyScope _scope;

    void Establish()
    {
        _label = EventSourceId.New();
        _scope = new(17UL, EventTypes: [new EventType("authority", 1)]);
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        await _handler.Handle(_commandContext, new EventsWithConcurrencyScopes([], [new(_label, _scope)]));
    }

    [Fact] void should_not_discard_the_scoped_batch() => _unitOfWork.AddEventsCallCount.ShouldEqual(1);
    [Fact] void should_preserve_the_scope_label() => _unitOfWork.AddedConcurrencyScopes.Single().Key.ShouldEqual(_label);
    [Fact] void should_preserve_the_required_revision() => _unitOfWork.AddedConcurrencyScopes.Single().Value.SequenceNumber.ShouldEqual(_scope.SequenceNumber);
    [Fact] void should_not_invent_events() => _unitOfWork.AddedEvents.ShouldBeEmpty();
}
