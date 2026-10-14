// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class with_failing_async_initialization : given.a_pipeline_with_async_scopes
{
    void Establish() => _asyncScope.BeginAsync(Arg.Any<CommandContext>()).Returns(Task.FromException(new Exception("initialization failed")));
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_record_the_failure() => _result.ExceptionMessages.ShouldContain("initialization failed");
    [Fact] void should_skip_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
    [Fact] void should_skip_remaining_async_initialization() => _secondAsyncScope.DidNotReceive().BeginAsync(Arg.Any<CommandContext>());
    [Fact] void should_complete_all_scopes_in_reverse() => _calls.ShouldEqual(["first begin", "sync begin", "second begin", "second complete", "sync complete", "first complete"]);
}
