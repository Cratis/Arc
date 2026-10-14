// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_beginning;

public class with_only_unguarded_parameters : given.a_guarded_transactional_scope
{
    void Establish() => _handler.Parameters.Returns(typeof(Commands.given.a_generated_target.CreatesUnguardedId).GetMethod("Handle")!.GetParameters());
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_not_require_contributor_support() => _unitOfWork.DidNotReceive().AddEnrollmentContributor(Arg.Any<IUnitOfWorkEnrollmentContributor>());
}
