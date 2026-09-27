// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_a_decision_conflicts : given.a_transactional_command_scope
{
    CommandResult _result;

    void Establish()
    {
        var (_, log, unit) = DecisionFixtures.Transaction();
        _unitOfWork = unit;
        _unitOfWorkManager.Begin(Arg.Any<Cratis.Execution.CorrelationId>()).Returns(unit);
        log.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<Cratis.Execution.CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(AppendManyResult.Failed(_correlationId, [
                new ConcurrencyViolation("source", (EventSequenceNumber)2ul, (EventSequenceNumber)3ul),
                new ConcurrencyViolation("other", (EventSequenceNumber)4ul, (EventSequenceNumber)5ul)]));
    }

    async Task Because()
    {
        _result = CommandResult.Success(_correlationId);
        _scope.Begin(_context);
        _unitOfWork.AddDecisionRead(DecisionFixtures.Protected<SampleModel>("source"));
        await _scope.Complete(_context, _result);
    }

    [Fact]
    void should_report_one_decision_conflict_without_sequence_numbers() =>
        _result.ValidationResults.Count(_ => _.Message.Contains("'source' changed")).ShouldEqual(1);

    [Fact]
    void should_retain_unrelated_concurrency_violations() =>
        _result.ValidationResults.Count(_ => _.Reason == ValidationResultReason.ConcurrencyViolation).ShouldEqual(2);

    [Fact]
    void should_not_expose_expected_or_actual_numbers_for_the_decision() =>
        (_result.ValidationResults.Single(_ => _.Message.Contains("changed after it was read")).State is ConcurrencyViolation).ShouldBeFalse();

    [Fact]
    void should_not_serialize_sequence_numbers_for_the_decision()
    {
        var decision = _result.ValidationResults.Single(_ => _.Message.Contains("changed after it was read"));
        var json = System.Text.Json.JsonSerializer.Serialize(decision);
        Assert.DoesNotContain("sequenceNumber", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expected", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("actual", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    void should_map_the_decision_conflict_to_http_bad_request() =>
        EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid)
            .ShouldEqual(System.Net.HttpStatusCode.BadRequest);

    public class SampleModel;
}
