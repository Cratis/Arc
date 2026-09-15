// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

public class when_an_unattributed_append_reports_an_error : given.an_observed_transactional_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        _appends.OnNext([new(null!, AppendResult.Failed(CorrelationId.NotSet, [new AppendError("unattributed failure")]))]);
        _result = CommandResult.Success(_correlationId);
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_preserve_the_existing_immediate_failure_contract() => _result.IsSuccess.ShouldBeFalse();

    [Fact] void should_not_authorize_recovery_by_ignoring_missing_attribution() =>
        _scope.GetCommitDisposition(_context).ShouldEqual(CommandCommitDisposition.Unknown);

    [Fact] void should_discard_pending_enrollment() => _unitOfWork.Received(1).Rollback();
}
