// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

public class when_an_append_has_another_correlation : given.an_observed_transactional_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        _appends.OnNext([new(null!, AppendResult.Failed(CorrelationId.New(), [new AppendError("foreign failure")]))]);
        _result = CommandResult.Success(_correlationId);
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_not_fail_this_command_for_the_foreign_append() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_not_make_this_commands_commit_uncertain() =>
        _scope.GetCommitDisposition(_context).ShouldEqual(CommandCommitDisposition.NotCommitted);

    [Fact] void should_complete_its_own_unit_of_work() => _unitOfWork.Received(1).Commit();
}
