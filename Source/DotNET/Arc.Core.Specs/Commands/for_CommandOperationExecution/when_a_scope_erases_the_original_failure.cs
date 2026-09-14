// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_a_scope_erases_the_original_failure : given.an_operation_pipeline
{
    bool _nextScopeSawFailure;

    void Establish()
    {
        _log.FailOn = "A";
        var erasingScope = Substitute.For<ICommandOperationExecutionScope>();
        erasingScope.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(call =>
        {
            ((CommandResult)call[1]).ExceptionMessages = [];
            return Task.CompletedTask;
        });
        var participant = Substitute.For<ICommandOperationExecutionScope>();
        participant.IsCommitParticipant.Returns(true);
        participant.GetCommitDisposition(Arg.Any<CommandContext>()).Returns(CommandCommitDisposition.NotCommitted);
        participant.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(call =>
        {
            _nextScopeSawFailure = !((CommandResult)call[1]).IsSuccess;
            return Task.CompletedTask;
        });
        _scopes = [participant, erasingScope];
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A")]));

    [Fact] void should_preserve_failure_before_the_commit_decision() => _nextScopeSawFailure.ShouldBeTrue();
    [Fact] void should_preserve_the_original_failure_for_the_caller() => _result.ExceptionMessages.ShouldEqual(["original:A"]);
    [Fact] void should_recover_from_the_frozen_failure_not_mutable_success() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Completed);
}
