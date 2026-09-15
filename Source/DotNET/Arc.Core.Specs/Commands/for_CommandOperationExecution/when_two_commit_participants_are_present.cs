// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_two_commit_participants_are_present : given.an_operation_pipeline
{
    void Establish()
    {
        var first = Substitute.For<ICommandOperationExecutionScope>();
        var second = Substitute.For<ICommandOperationExecutionScope>();
        first.IsCommitParticipant.Returns(true);
        second.IsCommitParticipant.Returns(true);
        _scopes = [first, second];
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A")]));

    [Fact] void should_reject_the_unsupported_boundary() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_start_no_external_work() => _log.Calls.ShouldBeEmpty();
}
