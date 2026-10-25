// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class and_the_delivery_is_redelivered : given.a_mixed_result_handler
{
    EventForEventSourceId[] _firstAttempt;
    EventForEventSourceId[] _redelivery;

    protected override IEventLog CreateEventLog()
    {
        var eventLog = Substitute.For<given.IdempotentEventLog>();
        eventLog.SupportsIdempotentAppends.Returns(true);
        return eventLog;
    }

    void Establish()
    {
        _context = _context with
        {
            Delivery = new ReactorDelivery("reactor", "orders", "default", EventSequenceId.Log, "observed", 7UL)
        };
    }

    async Task Because()
    {
        object[] effects = [new TestEvent("first"), new TestCommand("next"), new TestEvent("second")];
        await _dispatcher.Handle(_context, _store, effects);
        _firstAttempt = [.. _appended];
        _result = await _dispatcher.Handle(_context, _store, effects);
        _redelivery = [.. _appended];
    }

    [Fact] void should_key_every_appended_event() => _firstAttempt.All(_ => _.IdempotencyKey is not null).ShouldBeTrue();
    [Fact] void should_give_each_event_its_own_key() => _firstAttempt.Select(_ => _.IdempotencyKey).Distinct().Count().ShouldEqual(2);
    [Fact] void should_append_the_same_keys_on_redelivery() => _redelivery.Select(_ => _.IdempotencyKey).SequenceEqual(_firstAttempt.Select(_ => _.IdempotencyKey)).ShouldBeTrue();
    [Fact] void should_execute_the_commands_again() => _order.SequenceEqual(["append", "commands", "append", "commands"]).ShouldBeTrue();
}
