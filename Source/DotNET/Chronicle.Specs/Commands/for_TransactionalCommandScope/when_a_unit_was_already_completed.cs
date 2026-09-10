// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

public class when_a_unit_was_already_completed : given.a_transactional_command_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        _unitOfWork.IsCompleted.Returns(true);
        await _scope.Complete(_context, CommandResult.Error(_correlationId, "later failure"));
    }

    [Fact] void should_not_treat_completed_as_committed_or_rolled_back() => _scope.GetCommitDisposition(_context).ShouldEqual(CommandCommitDisposition.Unknown);
    [Fact] void should_not_attempt_a_second_rollback() => _unitOfWork.DidNotReceive().Rollback();
}
