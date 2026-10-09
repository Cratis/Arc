// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class and_the_commit_succeeds : given.a_pending_completion
{
    async Task Because()
    {
        _result.MergeWith(await Enroll());
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_commit_before_completing() => _operations.ShouldEqual(["commit", "complete"]);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
