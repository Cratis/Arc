// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class with_a_guarded_failed_command : given.a_guarded_transactional_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Error(_correlationId, "refused"));
    }

    [Fact] void should_dispose_the_registration() => _registration.Received(1).Dispose();
    [Fact] void should_roll_back_the_unit() => _unitOfWork.Received(1).Rollback();
}
