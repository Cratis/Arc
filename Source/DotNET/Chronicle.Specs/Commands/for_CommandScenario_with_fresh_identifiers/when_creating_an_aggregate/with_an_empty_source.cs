// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_creating_an_aggregate;

public class with_an_empty_source : given.a_generated_aggregate_scenario
{
    EventSequenceNumber _tail;

    async Task Because()
    {
        _result = await _scenario.Execute(_command);
        _tail = await _scenario.EventLog.GetTailSequenceNumber();
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_both_events() => _tail.ShouldEqual(EventSequenceNumber.First + 1);
    [Fact] void should_invoke_handle_once() => _command.Calls.ShouldEqual(1);
}
