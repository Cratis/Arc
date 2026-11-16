// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_an_invocation_scope_fails_to_begin : given.a_command_pipeline_with_invocation_scopes
{
    CommandResult _result;

    void Establish() => _second.When(_ => _.Begin(Arg.Any<CommandContext>())).Do(_ => throw new InvalidOperationException("Begin failed"));

    async Task Because() => _result = await _pipeline.Execute(_command, _serviceProvider);

    [Fact] void should_fold_the_failure_into_the_result() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_not_invoke_the_handler() => _commandHandler.DidNotReceive().Handle(Arg.Any<CommandContext>());
    [Fact] void should_not_begin_the_later_scope() => _third.DidNotReceive().Begin(Arg.Any<CommandContext>());
    [Fact] void should_complete_every_discovered_scope_when_operations_cannot_participate() => _third.Received(1).Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
    [Fact] void should_end_only_the_invocations_it_tried_to_begin() => _third.DidNotReceive().End(Arg.Any<CommandContext>());
    [Fact] void should_end_the_failed_invocation() => _second.Received(1).End(Arg.Any<CommandContext>());
    [Fact] void should_end_the_earlier_invocation() => _first.Received(1).End(Arg.Any<CommandContext>());
}
