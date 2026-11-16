// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_an_invocation_scope_fails_to_end : given.a_command_pipeline_with_invocation_scopes
{
    Exception _exception;

    void Establish() => _second.When(_ => _.End(Arg.Any<CommandContext>())).Do(_ => throw new InvalidOperationException("Cleanup failed"));

    async Task Because() => _exception = await Catch.Exception(() => _pipeline.Execute(_command, _serviceProvider));

    [Fact] void should_propagate_the_cleanup_failure() => _exception.Message.ShouldEqual("Cleanup failed");
    [Fact] void should_end_the_later_invocation() => _third.Received(1).End(Arg.Any<CommandContext>());
    [Fact] void should_end_the_earlier_invocation() => _first.Received(1).End(Arg.Any<CommandContext>());
}
