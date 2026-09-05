// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_the_unit_of_work_was_already_committed : given.a_transactional_command_scope
{
    void Establish() => _unitOfWork.IsCompleted.Returns(true);

    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_not_commit_again() => _unitOfWork.DidNotReceive().Commit();
    [Fact] void should_not_roll_back() => _unitOfWork.DidNotReceive().Rollback();
}
