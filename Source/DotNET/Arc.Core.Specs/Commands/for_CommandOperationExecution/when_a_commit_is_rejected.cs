// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_commit_is_rejected : given.an_operation_pipeline
{
    void Establish()
    {
        var participant = Substitute.For<ICommandOperationExecutionScope>();
        participant.IsCommitParticipant.Returns(true);
        participant.GetCommitDisposition(Arg.Any<CommandContext>()).Returns(CommandCommitDisposition.NotCommitted);
        participant.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(call =>
        {
            ((CommandResult)call[1]).MergeWith(CommandResult.Error(_correlationId, "commit rejected"));
            return Task.CompletedTask;
        });
        _scopes = [participant];
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A")]));

    [Fact] void should_compensate_after_known_rejection() => _log.Calls.ShouldEqual(["execute:A", "compensate:A"]);
    [Fact] void should_report_not_committed() => _result.Recovery.CommitDisposition.ShouldEqual(CommandCommitDisposition.NotCommitted);
    [Fact] void should_supply_scope_completion_failure_context() => _log.Failures[0].Source.ShouldEqual(CommandOperationFailureSource.ScopeCompletion);
}
