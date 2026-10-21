// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_executing_for_an_occurrence;

public class and_an_aggregate_commits_early : given.an_occurrence_execution
{
    bool _completedEarly;

    async Task Because()
    {
        BeginExecution();
        _scope.Begin(_context);
        _unit = CommandTransaction.Current;
        _exception = await Catch.Exception(() => TransactionalCommandScope.CommitAggregate(_unit));
        _completedEarly = _unit.IsCompleted;
        _scope.End(_context);
    }

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<OccurrenceCommandCannotCommitEarly>();
    [Fact] void should_not_commit_the_unit_of_work() => _completedEarly.ShouldBeFalse();
}
