// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_beginning;

/// <summary>
/// A command executed from within another command stacks its link on the outer command's, which is what makes
/// "the command one level up" answerable for anything reading the chain. The inner command runs in its own async
/// frame, the way the pipeline executes it, so the outer command's link survives the inner one completing.
/// </summary>
public class for_a_command_executed_from_another_command : given.a_command_causation_scope
{
    IImmutableList<Causation> _chainInsideTheInnerCommand;
    IImmutableList<Causation> _chainAfterTheInnerCommand;

    async Task Because()
    {
        var outer = ContextFor<SubmitExpenseReport>();
        _scope.Begin(outer);

        await RunInnerCommand();

        _chainAfterTheInnerCommand = _causationManager.GetCurrentChain();
        await _scope.Complete(outer, CommandResult.Success(outer.CorrelationId));
    }

    async Task RunInnerCommand()
    {
        var inner = ContextFor<ApproveExpenseReport>();
        _scope.Begin(inner);
        _chainInsideTheInnerCommand = _causationManager.GetCurrentChain();
        await _scope.Complete(inner, CommandResult.Success(inner.CorrelationId));
    }

    [Fact] void should_hold_both_commands_while_the_inner_one_runs() =>
        _chainInsideTheInnerCommand.Count(_ => _.Type == CommandCausation.Type).ShouldEqual(2);

    [Fact] void should_have_the_inner_command_last() =>
        _chainInsideTheInnerCommand[^1].Properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(ApproveExpenseReport));

    [Fact] void should_have_the_outer_command_one_level_up() =>
        _chainInsideTheInnerCommand[^2].Properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(SubmitExpenseReport));

    [Fact] void should_keep_the_outer_command_after_the_inner_one_completed() =>
        _chainAfterTheInnerCommand[^1].Properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(SubmitExpenseReport));

    [Fact] void should_drop_the_inner_command_after_it_completed() =>
        _chainAfterTheInnerCommand.Count(_ => _.Type == CommandCausation.Type).ShouldEqual(1);
}
