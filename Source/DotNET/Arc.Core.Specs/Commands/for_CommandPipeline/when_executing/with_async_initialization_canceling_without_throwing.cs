// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class with_async_initialization_canceling_without_throwing : given.a_pipeline_with_async_scopes
{
    readonly CancellationTokenSource _cancellation = new();

    void Establish() => _asyncScope.BeginAsync(Arg.Any<CommandContext>()).Returns(_ => _cancellation.CancelAsync());
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider, null, _cancellation.Token);
    void Destroy() => _cancellation.Dispose();

    [Fact] void should_stop_before_filters() => _commandFilters.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
    [Fact] void should_stop_remaining_async_initialization() => _secondAsyncScope.DidNotReceive().BeginAsync(Arg.Any<CommandContext>());
    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_complete_all_synchronous_scopes() => _calls.ShouldEqual(["first begin", "sync begin", "second begin", "second complete", "sync complete", "first complete"]);
}
