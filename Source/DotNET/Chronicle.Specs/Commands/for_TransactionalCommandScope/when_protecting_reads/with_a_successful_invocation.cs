// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads;

public class with_a_successful_invocation : given.a_read_protection_pipeline
{
    Exception? _duringFilters;
    Exception? _afterBorrowing;
    Exception? _afterExit;

    void Establish()
    {
        _filters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            _duringFilters = ProbeFrame();
            return CommandResult.Success(call.Arg<CommandContext>().CorrelationId);
        });
        _handle = _ =>
        {
            using (var lease = _unit.EnforceReadProtection()) { }
            _afterBorrowing = ProbeFrame();
            return Task.FromResult<object?>(null);
        };
    }

    async Task Because()
    {
        _result = await Execute();
        _afterExit = ProbeFrame();
    }

    [Fact] void should_enforce_before_filters() => _duringFilters.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_not_allow_a_borrower_to_end_enforcement() => _afterBorrowing.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_commit_without_decision_reads() => _unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_release_the_owner_at_invocation_exit() => _afterExit.ShouldBeNull();
}
