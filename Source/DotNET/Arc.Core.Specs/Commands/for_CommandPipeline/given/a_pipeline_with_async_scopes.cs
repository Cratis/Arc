// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_pipeline_with_async_scopes : a_command_pipeline_and_a_handler_for_command
{
    protected IAsyncCommandExecutionScope _asyncScope;
    protected IAsyncCommandExecutionScope _secondAsyncScope;
    protected CommandResult _result;
    protected List<string> _calls;

    void Establish()
    {
        _calls = [];
        _asyncScope = Substitute.For<IAsyncCommandExecutionScope>();
        _secondAsyncScope = Substitute.For<IAsyncCommandExecutionScope>();
        _serviceProvider.GetService(typeof(IInstancesOf<ICommandExecutionScope>))
            .Returns(new KnownInstancesOf<ICommandExecutionScope>([_asyncScope, _executionScope, _secondAsyncScope]));
        _asyncScope.When(_ => _.Begin(Arg.Any<CommandContext>())).Do(_ => _calls.Add("first begin"));
        _executionScope.When(_ => _.Begin(Arg.Any<CommandContext>())).Do(_ => _calls.Add("sync begin"));
        _secondAsyncScope.When(_ => _.Begin(Arg.Any<CommandContext>())).Do(_ => _calls.Add("second begin"));
        _asyncScope.BeginAsync(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _calls.Add("first async");
            return Task.CompletedTask;
        });
        _secondAsyncScope.BeginAsync(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _calls.Add("second async");
            return Task.CompletedTask;
        });
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _calls.Add("filters");
            return CommandResult.Success(_correlationId);
        });
        _asyncScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(_ =>
        {
            _calls.Add("first complete");
            return Task.CompletedTask;
        });
        _executionScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(_ =>
        {
            _calls.Add("sync complete");
            return Task.CompletedTask;
        });
        _secondAsyncScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(_ =>
        {
            _calls.Add("second complete");
            return Task.CompletedTask;
        });
    }
}
