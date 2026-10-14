// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_beginning;

public class without_guarded_parameters : given.a_guarded_transactional_scope
{
    void Establish() => _handler.Parameters.Returns(typeof(object).GetMethod(nameof(ToString))!.GetParameters());
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_not_register_a_contributor() => _unitOfWork.DidNotReceive().AddEnrollmentContributor(Arg.Any<IUnitOfWorkEnrollmentContributor>());
    [Fact] void should_still_commit() => _unitOfWork.Received(1).Commit();
}
