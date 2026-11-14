// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_a_response_value_handler_appends;

/// <summary>
/// An inner command completes its own scopes while the outer command is still running; its completion must not leave
/// the outer command's causation wrong, and the work deferred to the outer command's commit carries the outer command.
/// </summary>
public class in_a_nested_command : given.a_command_with_a_response_value_handler
{
    IImmutableList<Causation> _chainAfterInner;

    async Task Because() => await Execute(
        [new TransactionalCommandScope(), new CommandCausationScope()],
        async () =>
        {
            await ExecuteInner();
            _chainAfterInner = _causationManager.GetCurrentChain();
        });

    [Fact] void should_leave_only_the_outer_command_on_the_chain_after_the_inner_completes() =>
        _chainAfterInner.Where(_ => _.Type == CommandCausation.Type).Select(_ => _.Properties[CommandCausation.CommandTypeProperty]).ShouldContainOnly(nameof(PayInvoice));

    [Fact] void should_run_the_deferred_work_under_the_outer_command() =>
        CommandLink(_chainAtCompletion).Properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(PayInvoice));

    async Task ExecuteInner()
    {
        var inner = _context with { Type = typeof(SendReceipt), Command = new SendReceipt(), Values = new() };
        ICommandExecutionScope[] scopes = [new TransactionalCommandScope(), new CommandCausationScope()];
        foreach (var scope in scopes)
        {
            scope.Begin(inner);
        }

        await new CompleteStreamCommandResponseValueHandler(_eventLog).Handle(inner, new CompleteStream("completion", "period"));

        for (var index = scopes.Length - 1; index >= 0; index--)
        {
            await scopes[index].Complete(inner, _result);
        }
    }

    sealed record SendReceipt;
}
