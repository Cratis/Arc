// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_a_decision_conflicts : given.a_transactional_command_scope
{
    CommandResult _result;

    void Establish()
    {
        _unitOfWork.GetDecisionConflicts().Returns([new DecisionConflict(typeof(SampleModel), (ReadModelKey)"source")]);
        _unitOfWork.GetConcurrencyViolations().Returns([
            new ConcurrencyViolation("source", (EventSequenceNumber)2ul, (EventSequenceNumber)3ul),
            new ConcurrencyViolation("other", (EventSequenceNumber)4ul, (EventSequenceNumber)5ul)]);
    }

    async Task Because()
    {
        _result = CommandResult.Success(_correlationId);
        _scope.Begin(_context);
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_report_one_decision_conflict_without_sequence_numbers() =>
        _result.ValidationResults.Count(_ => _.Message.Contains("source")).ShouldEqual(1);

    [Fact] void should_retain_unrelated_concurrency_violations() =>
        _result.ValidationResults.Count(_ => _.Reason == ValidationResultReason.ConcurrencyViolation).ShouldEqual(2);

    [Fact] void should_not_expose_expected_or_actual_numbers_for_the_decision() =>
        (_result.ValidationResults.Single(_ => _.Message.Contains("changed after it was read")).State is ConcurrencyViolation).ShouldBeFalse();

    public class SampleModel;
}
