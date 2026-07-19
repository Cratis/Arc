// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_the_command_succeeded : given.a_transactional_command_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_begin_the_unit_of_work_with_the_command_correlation() => _unitOfWorkManager.Received(1).Begin(_correlationId);
    [Fact] void should_commit_the_unit_of_work() => _unitOfWork.Received(1).Commit();
    [Fact] void should_not_roll_back() => _unitOfWork.DidNotReceive().Rollback();
}
