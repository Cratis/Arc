// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class with_cancellation_before_async_initialization : given.a_pipeline_with_async_scopes
{
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider, null, new CancellationToken(true));

    [Fact] void should_skip_async_initialization() => _asyncScope.DidNotReceive().BeginAsync(Arg.Any<CommandContext>());
    [Fact] void should_skip_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_complete_synchronous_scopes() => _calls.ShouldEqual(["first begin", "sync begin", "second begin", "second complete", "sync complete", "first complete"]);
}
