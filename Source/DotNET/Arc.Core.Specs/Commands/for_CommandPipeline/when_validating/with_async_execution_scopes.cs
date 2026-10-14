// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class with_async_execution_scopes : given.a_pipeline_with_async_scopes
{
    async Task Because() => _result = await _commandPipeline.Validate(_command, _serviceProvider);

    [Fact] void should_only_run_filters() => _calls.ShouldEqual(["filters"]);
    [Fact] void should_skip_async_initialization() => _asyncScope.DidNotReceive().BeginAsync(Arg.Any<CommandContext>());
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
