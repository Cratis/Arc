// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class and_the_commit_conflicts : given.a_pending_completion
{
    void Establish()
    {
        _unitOfWork.GetConcurrencyViolations().Returns([new ConcurrencyViolation(EventSourceId.New(), EventSequenceNumber.First, new(1))]);
    }

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_not_complete_the_stream() => _operations.ShouldEqual(["commit"]);
    [Fact] void should_refuse_as_validation() => _result.IsValid.ShouldBeFalse();
}
