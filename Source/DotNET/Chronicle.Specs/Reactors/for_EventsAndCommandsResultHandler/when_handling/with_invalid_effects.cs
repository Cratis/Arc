// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class with_invalid_effects : given.a_mixed_result_handler
{
    bool[] _successes;

    async Task Because()
    {
        object[] leaves = [null!, new object(), new AmbiguousCommand(), new CompleteStream(), new EventForEventSourceId("explicit", new object())];
        var results = new List<bool>();
        foreach (var leaf in leaves)
        {
            var result = await _handler.Handle(_context, _store, new object[] { new TestEvent("bare"), new TestCommand("next"), leaf });
            results.Add(result.IsSuccess);
        }
        _successes = results.ToArray();
    }

    [Fact] void should_revalidate_and_fail_every_invalid_sequence() => _successes.Any(success => success).ShouldBeFalse();
    [Fact] void should_perform_no_effects() => _order.ShouldBeEmpty();
}
