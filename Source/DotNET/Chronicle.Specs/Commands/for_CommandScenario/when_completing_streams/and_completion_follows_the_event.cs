// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

public class and_completion_follows_the_event : given.a_period_completion
{
    async Task Because() => _result = await _scenario.Execute(new(_id));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_append_the_event_before_closing() => await AssertAppended();
    [Fact] async Task should_complete_the_routed_stream() => await AssertCompleted();
    [Fact] void should_preserve_the_response() => ((CommandResult<string>)_result).Response.ShouldEqual("response");
}
