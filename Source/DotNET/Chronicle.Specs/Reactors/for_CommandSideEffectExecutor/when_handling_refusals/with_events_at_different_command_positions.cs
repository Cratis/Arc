// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_events_at_different_command_positions : given.a_refusal_with_delivery
{
    IdempotencyKey?[] _firstKeys;
    IdempotencyKey?[] _secondKeys;

    void Establish() => _branch = new[] { new RefusalRecorded("first"), new RefusalRecorded("second") };
    async Task Because()
    {
        await _executor.Execute([_invocation], _context, _store);
        _firstKeys = _appended.Select(envelope => envelope.IdempotencyKey).ToArray();
        _result = await _executor.Execute([_later, _invocation], _context, _store);
        _secondKeys = _appended.Select(envelope => envelope.IdempotencyKey).ToArray();
        await _executor.Execute([_later, _invocation], _context, _store);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_distinguish_command_positions() => _firstKeys[0].ShouldNotEqual(_secondKeys[0]);
    [Fact] void should_distinguish_event_positions() => _secondKeys[0].ShouldNotEqual(_secondKeys[1]);
    [Fact] void should_use_command_and_event_ordinals() => _secondKeys[1].ShouldEqual(_context.Delivery!.IdempotencyKeyFor("arc:refusal:1:1"));
    [Fact] void should_keep_retry_keys_stable() => _appended.Select(envelope => envelope.IdempotencyKey).SequenceEqual(_secondKeys).ShouldBeTrue();
    [Fact] void should_append_each_position_once() => _trackedLog.Appended.Count.ShouldEqual(4);
}
