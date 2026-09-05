// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_a_foreign_unit_of_work_is_current : given.a_transactional_command_scope
{
    IUnitOfWork _foreignUnitOfWork;

    void Establish()
    {
        _foreignUnitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWorkManager.HasCurrent.Returns(true);
        _unitOfWorkManager.Current.Returns(_foreignUnitOfWork);
    }

    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_begin_its_own_unit_of_work() => _unitOfWorkManager.Received(1).Begin(_correlationId);
    [Fact] void should_commit_its_own_unit_of_work() => _unitOfWork.Received(1).Commit();
    [Fact] void should_leave_the_foreign_unit_of_work_untouched() => _foreignUnitOfWork.DidNotReceive().Commit();
}
