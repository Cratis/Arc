// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_executing;

public class with_stream_type_concurrency_and_multiple_fresh_events : given.an_attributed_generated_source<given.attributed_source_commands.WithStreamType>
{
    void Establish() => _command.Shape = given.a_fresh_identifier_scenario.ResultShape.Wrappers;
    async Task Because() => await Execute();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_both_events() => _tailAfter.Value.ShouldEqual(1UL);
    [Fact] void should_invoke_handle_once() => _command.Calls.ShouldEqual(1);
}
