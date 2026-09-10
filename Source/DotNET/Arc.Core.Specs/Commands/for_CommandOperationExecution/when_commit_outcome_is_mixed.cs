// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_commit_outcome_is_mixed : given.an_operation_pipeline
{
    void Establish()
    {
        var participant = Substitute.For<ICommandOperationExecutionScope>();
        participant.IsCommitParticipant.Returns(true);
        participant.GetCommitDisposition(Arg.Any<CommandContext>()).Returns(CommandCommitDisposition.NotCommitted, CommandCommitDisposition.Mixed);
        participant.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(Task.FromException(new InvalidCommandOperation("partial commit")));
        _scopes = [participant];
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A")]));

    [Fact] void should_not_blanket_reverse_mixed_business_facts() => _log.Calls.ShouldEqual(["execute:A"]);
    [Fact] void should_report_indeterminate_recovery() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Indeterminate);
    [Fact] void should_preserve_mixed_disposition() => _result.Recovery.CommitDisposition.ShouldEqual(CommandCommitDisposition.Mixed);
}
