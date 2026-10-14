// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class with_a_guarded_owner : given.a_guarded_transactional_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_register_once() => _unitOfWork.Received(1).AddEnrollmentContributor(Arg.Any<IUnitOfWorkEnrollmentContributor>());
    [Fact] void should_dispose_once() => _registration.Received(1).Dispose();
    [Fact] void should_commit_once() => _unitOfWork.Received(1).Commit();
}
