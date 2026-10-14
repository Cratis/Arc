// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class with_a_throwing_guarded_commit : given.a_guarded_transactional_scope
{
    void Establish() => _unitOfWork.Commit().Returns(Task.FromException(new InvalidCommandOperation("commit failed")));
    async Task Because()
    {
        _scope.Begin(_context);
        _error = await Catch.Exception(() => _scope.Complete(_context, CommandResult.Success(_correlationId)));
    }

    [Fact] void should_preserve_the_failure() => _error.ShouldBeOfExactType<InvalidCommandOperation>();
    [Fact] void should_dispose_the_registration() => _registration.Received(1).Dispose();
}
