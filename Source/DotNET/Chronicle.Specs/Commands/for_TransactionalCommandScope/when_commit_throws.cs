// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope;

public class when_commit_throws : given.a_transactional_command_scope
{
    Exception _exception;

    void Establish() => _unitOfWork.Commit().Returns(Task.FromException(new InvalidCommandOperation("lost acknowledgment")));
    async Task Because()
    {
        _scope.Begin(_context);
        _exception = await Catch.Exception(() => _scope.Complete(_context, CommandResult.Success(_correlationId)));
    }

    [Fact] void should_preserve_the_original_exception() => _exception.Message.ShouldEqual("lost acknowledgment");
    [Fact] void should_not_infer_commit_failure_from_completion() => _scope.GetCommitDisposition(_context).ShouldEqual(CommandCommitDisposition.Unknown);
}
