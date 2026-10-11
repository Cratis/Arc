// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_checking_can_handle;

public class with_invalid_leaves : given.a_mixed_result_handler
{
    bool[] _matches;

    void Because()
    {
        object[] invalidLeaves =
        [
            null!, new object(), new AmbiguousCommand(), new CompleteStream(),
            new EventForEventSourceId("explicit", new object()),
            new EventForEventSourceId("explicit", null!),
            new EventForEventSourceId("explicit", new AmbiguousCommand()),
            new EventForEventSourceId("explicit", new CompleteStream())
        ];
        _matches = invalidLeaves.Select(leaf => _handler.CanHandle(_context, _store, new object[] { new TestEvent("valid"), new TestCommand("valid"), leaf })).ToArray();
    }

    [Fact] void should_reject_every_invalid_sequence() => _matches.Any(match => match).ShouldBeFalse();
    [Fact] void should_reject_a_null_result() => _handler.CanHandle(_context, _store, null!).ShouldBeFalse();
    [Fact] void should_not_guess_without_a_context_store() => _handler.CanHandle(_context with { EventStore = null }, new object[] { new TestEvent("valid"), new TestCommand("valid") }).ShouldBeFalse();
}
