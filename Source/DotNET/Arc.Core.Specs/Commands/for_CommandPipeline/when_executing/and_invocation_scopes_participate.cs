// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_invocation_scopes_participate : given.a_command_pipeline_with_invocation_scopes
{
    CommandResult _result;

    async Task Because() => _result = await _pipeline.Execute(_command, _serviceProvider);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_end_every_invocation_after_completing_in_reverse_order() => Received.InOrder(() =>
    {
        _first.Begin(Arg.Any<CommandContext>());
        _second.Begin(Arg.Any<CommandContext>());
        _third.Begin(Arg.Any<CommandContext>());
        _third.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
        _second.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
        _first.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
        _third.End(Arg.Any<CommandContext>());
        _second.End(Arg.Any<CommandContext>());
        _first.End(Arg.Any<CommandContext>());
    });
}
