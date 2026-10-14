// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_source_type_concurrency_on_a_fresh_source : given.an_attributed_generated_source<given.attributed_source_commands.WithSourceType>
{
    void Establish() => _command.Shape = given.a_fresh_identifier_scenario.ResultShape.Wrapper;
    async Task Because() => await Execute();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_the_event() => _tailAfter.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_invoke_handle_once() => _command.Calls.ShouldEqual(1);
}
