// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class and_a_refusal_delivery_is_repeated : given.a_mixed_refusal
{
    int _logicalEventsAfterRetry;
    bool _epochSucceeded;
    bool _changedDeliverySucceeded;

    async Task Because()
    {
        await DeliverTwice();
        _logicalEventsAfterRetry = _trackedLog.Appended.Count;
        var effects = _events.Concat([_invocation]).ToArray();
        _epochSucceeded = (await _handler.Handle(_context with { Delivery = _context.Delivery! with { EffectEpoch = 1 } }, _store, effects)).IsSuccess;
        _changedDeliverySucceeded = (await _handler.Handle(_context with { Delivery = _context.Delivery! with { SequenceNumber = 43 } }, _store, effects)).IsSuccess;
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_redeliver_without_conflict() => _retry.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_two_events_only_once() => _logicalEventsAfterRetry.ShouldEqual(2);
    [Fact] void should_keep_direct_keys_stable() => _trackedLog.Attempts[0][0].IdempotencyKey.ShouldEqual(_trackedLog.Attempts[2][0].IdempotencyKey);
    [Fact] void should_keep_branch_keys_stable() => _trackedLog.Attempts[1][0].IdempotencyKey.ShouldEqual(_trackedLog.Attempts[3][0].IdempotencyKey);
    [Fact] void should_allow_a_changed_epoch() => _epochSucceeded.ShouldBeTrue();
    [Fact] void should_allow_a_changed_delivery() => _changedDeliverySucceeded.ShouldBeTrue();
    [Fact] void should_separate_changed_epochs() => _trackedLog.Attempts[1][0].IdempotencyKey.ShouldNotEqual(_trackedLog.Attempts[5][0].IdempotencyKey);
    [Fact] void should_separate_changed_deliveries() => _trackedLog.Attempts[1][0].IdempotencyKey.ShouldNotEqual(_trackedLog.Attempts[7][0].IdempotencyKey);
    [Fact] void should_keep_content_and_routes_stable() => _trackedLog.Attempts[1][0].ShouldEqual(_trackedLog.Attempts[3][0]);
}
