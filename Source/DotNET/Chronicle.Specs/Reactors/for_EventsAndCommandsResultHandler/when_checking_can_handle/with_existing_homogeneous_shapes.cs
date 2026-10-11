// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_checking_can_handle;

public class with_existing_homogeneous_shapes : given.a_mixed_result_handler
{
    IReactorSideEffectHandler[][] _before;
    IReactorSideEffectHandler[][] _after;
    bool[] _newMatches;

    void Because()
    {
        object[] values =
        [
            new TestEvent("single"), new TestCommand("single"), new EventForEventSourceId("explicit", new TestEvent("single")), new CompleteStream(),
            new object[] { new TestEvent("first"), new TestEvent("second") },
            new object[] { new TestCommand("first"), new TestCommand("second") },
            new object[] { new EventForEventSourceId("explicit", new TestEvent("routed")) },
            new object[] { new TestEvent("bare"), new EventForEventSourceId("explicit", new TestEvent("routed")) },
            Array.Empty<object>()
        ];
        _before = values.Select(value => _existingHandlers.Where(handler => handler.CanHandle(_context, _store, value)).ToArray()).ToArray();
        _after = values.Select(value => _existingHandlers.Append(_handler).Where(handler => handler.CanHandle(_context, _store, value)).ToArray()).ToArray();
        _newMatches = values.Select(value => _handler.CanHandle(_context, _store, value)).ToArray();
    }

    [Fact] void should_preserve_all_pre_existing_matching_ownership() => _before.Zip(_after).All(pair => pair.First.SequenceEqual(pair.Second)).ShouldBeTrue();
    [Fact] void should_leave_every_shape_to_its_existing_handlers() => _newMatches.Any(match => match).ShouldBeFalse();
}
