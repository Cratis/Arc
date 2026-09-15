// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_commit_acknowledgment_is_unknown : given.an_operation_pipeline
{
    void Establish()
    {
        var participant = Substitute.For<ICommandOperationExecutionScope>();
        participant.IsCommitParticipant.Returns(true);
        participant.GetCommitDisposition(Arg.Any<CommandContext>()).Returns(CommandCommitDisposition.NotCommitted, CommandCommitDisposition.Unknown);
        participant.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(Task.FromException(new InvalidCommandOperation("lost acknowledgment")));
        _scopes = [participant];
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A")]));

    [Fact] void should_not_guess_that_reversal_is_safe() => _log.Calls.ShouldEqual(["execute:A"]);
    [Fact] void should_report_indeterminate_recovery() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Indeterminate);
    [Fact] void should_preserve_the_original_failure() => _result.ExceptionMessages.ShouldEqual(["lost acknowledgment"]);
}
