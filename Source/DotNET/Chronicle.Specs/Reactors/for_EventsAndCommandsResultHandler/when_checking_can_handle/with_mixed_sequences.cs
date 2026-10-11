// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_checking_can_handle;

public class with_mixed_sequences : given.a_mixed_result_handler
{
    IReactorSideEffectHandler[][] _matches;
    bool[] _legacyMatches;

    void Because()
    {
        object[] sequences =
        [
            new object[] { new TestEvent("bare"), new TestCommand("first") },
            new object[] { new EventForEventSourceId("explicit", new TestEvent("routed")), new TestCommand("first") },
            new List<object> { new TestCommand("first"), new TestEvent("bare"), new EventForEventSourceId("explicit", new TestEvent("routed")), new TestCommand("second") }
        ];
        _matches = sequences.Select(value => _existingHandlers.Append(_handler).Where(handler => handler.CanHandle(_context, _store, value)).ToArray()).ToArray();
        _legacyMatches = sequences.Select(value => _handler.CanHandle(_context, value)).ToArray();
    }

    [Fact] void should_match_exactly_one_dispatcher_handler_for_each_mixed_shape() => _matches.All(matches => matches.SequenceEqual([_handler])).ShouldBeTrue();
    [Fact] void should_use_the_context_store_for_the_legacy_contract() => _legacyMatches.All(match => match).ShouldBeTrue();
}
