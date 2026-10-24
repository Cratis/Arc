// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_beginning;

public class under_a_controller_command : given.a_transactional_command_scope
{
    IUnitOfWork _inner;
    CommandContext _innerContext;
    bool _ownedInnerUnit;
    bool _restoredController;

    void Establish()
    {
        _context = _context with { IsControllerAction = true };
        _innerContext = _context with { IsControllerAction = false, Values = new() };
        _inner = Substitute.For<IUnitOfWork>();
        _unitOfWorkManager.Begin(_correlationId).Returns(_unitOfWork, _inner);
    }

    async Task Because()
    {
        _scope.Begin(_context);
        _scope.Begin(_innerContext);
        _ownedInnerUnit = ReferenceEquals(CommandTransaction.Current, _inner);
        await _scope.Complete(_innerContext, CommandResult.Success(_correlationId));
        _scope.End(_innerContext);
        _restoredController = ReferenceEquals(CommandTransaction.Current, _unitOfWork);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
        _scope.End(_context);
    }

    [Fact] void should_begin_an_independent_unit() => _ownedInnerUnit.ShouldBeTrue();
    [Fact] void should_commit_the_inner_unit() => _inner.Received(1).Commit();
    [Fact] void should_restore_the_controller_transaction() => _restoredController.ShouldBeTrue();
    [Fact] void should_restore_the_manager_lookup() => _unitOfWorkManager.Received(1).SetCurrent(_unitOfWork);
    [Fact] void should_commit_the_controller_unit() => _unitOfWork.Received(1).Commit();
}
