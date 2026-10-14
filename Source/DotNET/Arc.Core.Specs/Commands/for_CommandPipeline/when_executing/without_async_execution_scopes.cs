// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class without_async_execution_scopes : given.a_command_pipeline_and_a_handler_for_command
{
    readonly List<string> _calls = [];
    CommandResult _result;

    void Establish()
    {
        _commandContextValuesBuilder.Build(_command).Returns(_ =>
        {
            _calls.Add("context values");
            return new CommandContextValues();
        });
        _executionScope.When(_ => _.Begin(Arg.Any<CommandContext>())).Do(_ => _calls.Add("begin"));
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _calls.Add("filters");
            return CommandResult.Success(_correlationId);
        });
        _commandHandler.Handle(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _calls.Add("handle");
            return new ValueTask<object?>((object?)null);
        });
        _executionScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(_ =>
        {
            _calls.Add("complete");
            return Task.CompletedTask;
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_preserve_the_existing_lifecycle() => _calls.ShouldEqual(["context values", "begin", "filters", "handle", "complete"]);
    [Fact] void should_preserve_success() => _result.IsSuccess.ShouldBeTrue();
}
