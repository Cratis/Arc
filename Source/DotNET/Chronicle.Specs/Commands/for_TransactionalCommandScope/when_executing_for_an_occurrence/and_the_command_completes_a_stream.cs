// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Occurrences;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_executing_for_an_occurrence;

public class and_the_command_completes_a_stream : given.an_occurrence_execution
{
    void Because()
    {
        BeginExecution();
        _scope.Begin(_context);
        _exception = Catch.Exception(() => TransactionalCommandScope.EnrollCompletion(_context, new PendingStreamCompletion(_store.EventLog, EventStreamType.All, EventStreamId.Default, null)));
    }

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<OccurrenceCommandCannotCompleteStreams>();
}
