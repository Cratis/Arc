// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

using static Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.given.direct_write_commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario_with_fresh_identifiers.when_writing_directly;

public class with_a_repeated_pin : given.a_direct_write_scenario<DirectWrite>
{
    CommandResult _first;
    async Task Because()
    {
        await Execute(new(WriteStyle.AddEvent));
        _first = _result;
        await Execute(new(WriteStyle.AddEvent));
    }

    [Fact] void should_succeed_for_the_first_invocation() => _first.IsSuccess.ShouldBeTrue();
    [Fact] void should_reject_the_second_invocation() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_classify_the_new_invocations_collision() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
    [Fact] void should_not_continue_the_original_source() => _after.ShouldEqual(_before);
}
