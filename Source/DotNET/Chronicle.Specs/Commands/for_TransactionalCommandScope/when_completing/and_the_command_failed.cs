// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_the_command_failed : given.a_transactional_command_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Error(_correlationId, "Something failed"));
    }

    [Fact] void should_roll_back_the_unit_of_work() => _unitOfWork.Received(1).Rollback();
    [Fact] void should_not_commit() => _unitOfWork.DidNotReceive().Commit();
}
