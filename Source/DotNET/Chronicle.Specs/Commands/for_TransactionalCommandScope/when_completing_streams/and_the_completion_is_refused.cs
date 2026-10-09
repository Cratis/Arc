// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class and_the_completion_is_refused : given.a_pending_completion
{
    void Establish()
    {
        Result<EventSequenceNumber, CompleteStreamError> refused = CompleteStreamError.DefaultStreamCannotBeCompleted;
        _eventLog.CompleteStream("completion", "period").Returns(refused);
    }

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_name_the_stream() => _result.ValidationResults.Single().Message.ShouldContain("completion/period");
    [Fact] void should_not_undo_the_committed_events() => _operations.ShouldEqual(["commit"]);
}
