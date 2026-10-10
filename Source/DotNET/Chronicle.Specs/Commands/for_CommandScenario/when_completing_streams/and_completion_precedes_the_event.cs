// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

public class and_completion_precedes_the_event : given.a_period_completion
{
    void Establish() => _scenario.UseDecisionReads();
    async Task Because() => _result = await _scenario.Execute(new(_id, CompletionFirst: true));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_append_the_event_before_closing() => await AssertAppended();
    [Fact] async Task should_record_completion_in_decision_mode() => await AssertCompleted();
}
