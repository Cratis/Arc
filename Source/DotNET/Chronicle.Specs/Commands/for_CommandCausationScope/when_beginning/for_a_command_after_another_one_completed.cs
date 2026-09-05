// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_beginning;

/// <summary>
/// Two commands run one after the other - a reactor executing two of them, a job looping - are siblings, not
/// parent and child. The second must not read as caused by the first, or anything mining the chain learns an
/// ordering that never existed.
/// </summary>
public class for_a_command_after_another_one_completed : given.a_command_causation_scope
{
    IImmutableList<Causation> _chain;
    IImmutableList<Causation> _chainAfterBoth;

    async Task Because()
    {
        var first = ContextFor<SubmitExpenseReport>();
        _scope.Begin(first);
        await _scope.Complete(first, CommandResult.Success(first.CorrelationId));

        var second = ContextFor<ApproveExpenseReport>();
        _scope.Begin(second);
        _chain = _causationManager.GetCurrentChain();
        await _scope.Complete(second, CommandResult.Success(second.CorrelationId));

        _chainAfterBoth = _causationManager.GetCurrentChain();
    }

    [Fact] void should_record_the_second_command() =>
        _chain.Last(_ => _.Type == CommandCausation.Type).Properties[CommandCausation.CommandTypeProperty].ShouldEqual(nameof(ApproveExpenseReport));

    [Fact] void should_not_leave_the_first_command_on_the_chain() =>
        _chain.Count(_ => _.Type == CommandCausation.Type).ShouldEqual(1);

    [Fact] void should_leave_no_command_on_the_chain_once_both_completed() =>
        _chainAfterBoth.Any(_ => _.Type == CommandCausation.Type).ShouldBeFalse();
}
