// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_later_scope_fails_after_commit : given.an_operation_pipeline
{
    void Establish()
    {
        var participant = Substitute.For<ICommandOperationExecutionScope>();
        participant.IsCommitParticipant.Returns(true);
        participant.GetCommitDisposition(Arg.Any<CommandContext>()).Returns(CommandCommitDisposition.NotCommitted, CommandCommitDisposition.Committed);
        var laterScope = Substitute.For<ICommandOperationExecutionScope>();
        laterScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(Task.FromException(new InvalidCommandOperation("cleanup failed")));
        _scopes = [laterScope, participant];
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A")]));

    [Fact] void should_not_reverse_committed_business_work() => _log.Calls.ShouldEqual(["execute:A"]);
    [Fact] void should_report_suppressed_recovery() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Suppressed);
    [Fact] void should_preserve_the_completion_failure() => _result.IsSuccess.ShouldBeFalse();
}
