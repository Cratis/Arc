// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

public class when_reporting_commit_disposition : given.a_transactional_command_scope
{
    CommandCommitDisposition _disposition;

    void Establish() => _unitOfWork.GetEvents().Returns([new object()]);
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
        _disposition = _scope.GetCommitDisposition(_context);
    }

    [Fact] void should_report_an_acknowledged_commit() => _disposition.ShouldEqual(CommandCommitDisposition.Committed);
    [Fact] void should_opt_into_a_single_deferred_participant() => _scope.IsCommitParticipant.ShouldBeTrue();
}
