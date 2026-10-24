// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandActionFilter.given;

public class a_post_request_with_execution_scopes : a_post_request_for_a_command_action
{
    protected IAsyncCommandExecutionScope _first;
    protected IAsyncCommandExecutionScope _second;
    protected List<string> _calls;
    protected CommandContext _scopeContext;
    protected CommandResult _completedResult;
    protected AsyncLocal<string> _ambient;
    ServiceProvider _services;

    protected new CommandResult<object> ResultingCommandResult => (CommandResult<object>)((ObjectResult)(_actionContext.Result ?? _executedContext.Result)).Value!;

    void Establish()
    {
        _calls = [];
        _ambient = new();
        _first = CreateScope("first");
        _second = CreateScope("second");
        _first.When(scope => scope.Begin(Arg.Any<CommandContext>())).Do(call =>
        {
            _scopeContext = call.Arg<CommandContext>();
            _ambient.Value = "active";
        });
        var instances = Substitute.For<IInstancesOf<ICommandExecutionScope>>();
        instances.GetEnumerator().Returns(_ => new ICommandExecutionScope[] { _first, _second }.AsEnumerable().GetEnumerator());
        _services = new ServiceCollection().AddSingleton(instances).BuildServiceProvider();
        _actionContext.HttpContext.RequestServices = _services;
        _executedContext.Result = new ObjectResult("response");
    }

    void Cleanup() => _services.Dispose();

    IAsyncCommandExecutionScope CreateScope(string name)
    {
        var scope = Substitute.For<IAsyncCommandExecutionScope, ICommandInvocationExecutionScope>();
        scope.When(_ => _.Begin(Arg.Any<CommandContext>())).Do(_ => _calls.Add($"{name}.begin"));
        scope.BeginAsync(Arg.Any<CommandContext>()).Returns(_ =>
        {
            _calls.Add($"{name}.beginAsync");
            return Task.CompletedTask;
        });
        scope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(call =>
        {
            _calls.Add($"{name}.complete:{_ambient.Value}");
            _completedResult = call.Arg<CommandResult>();
            return Task.CompletedTask;
        });
        ((ICommandInvocationExecutionScope)scope).When(_ => _.End(Arg.Any<CommandContext>())).Do(_ => _calls.Add($"{name}.end:{_ambient.Value}"));
        return scope;
    }
}
