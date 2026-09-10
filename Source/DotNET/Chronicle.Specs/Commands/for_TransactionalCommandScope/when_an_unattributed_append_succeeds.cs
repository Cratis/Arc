// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

public class when_an_unattributed_append_succeeds : given.an_observed_transactional_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        _appends.OnNext([new(null!, AppendResult.Success(CorrelationId.NotSet, new EventSequenceNumber(42)))]);
        _result = CommandResult.Error(_correlationId, "later command failure");
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_not_assume_the_successful_append_is_safe_to_reverse() =>
        _scope.GetCommitDisposition(_context).ShouldEqual(CommandCommitDisposition.Committed);

    [Fact] void should_still_roll_back_pending_enrollment() => _unitOfWork.Received(1).Rollback();

    [Fact] void should_preserve_the_command_failure() => _result.IsSuccess.ShouldBeFalse();
}
