// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_a_unit_of_work_is_already_current : given.a_transactional_command_scope
{
    void Establish()
    {
        _unitOfWorkManager.HasCurrent.Returns(true);
        _unitOfWorkManager.Current.Returns(_unitOfWork);
    }

    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_not_begin_a_new_unit_of_work() => _unitOfWorkManager.DidNotReceive().Begin(Arg.Any<CorrelationId>());
    [Fact] void should_leave_committing_to_the_owner() => _unitOfWork.DidNotReceive().Commit();
    [Fact] void should_not_roll_back() => _unitOfWork.DidNotReceive().Rollback();
}
