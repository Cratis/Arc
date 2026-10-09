// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class and_the_commit_outcome_is_unknown : given.a_pending_completion
{
    async Task Because()
    {
        _result.MergeWith(await Enroll());
        _unitOfWork.IsCompleted.Returns(true);
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_not_complete_the_stream() => _operations.ShouldBeEmpty();
    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_unknown_outcome() => _result.ExceptionMessages.Single().ShouldContain("commit outcome is unknown");
}
