// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_two_events_for_one_new_source : given.a_fresh_identifier_scenario
{
    void Establish() => _command = new(ResultShape.Wrappers);

    async Task Because() => await Execute();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_both_events() => _tailAfter.ShouldEqual(EventSequenceNumber.First + 1);
    [Fact] void should_invoke_handle_only_once() => _command.Calls.ShouldEqual(1);
}
