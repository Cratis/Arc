// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

public class when_reporting_rollback : given.a_transactional_command_scope
{
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Error(_correlationId, "operation failed"));
    }

    [Fact] void should_report_known_noncommit() => _scope.GetCommitDisposition(_context).ShouldEqual(CommandCommitDisposition.NotCommitted);
    [Fact] void should_rollback_pending_enrollment() => _unitOfWork.Received(1).Rollback();
}
