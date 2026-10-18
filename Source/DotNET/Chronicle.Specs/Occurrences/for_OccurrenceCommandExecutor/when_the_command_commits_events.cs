// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor;

public class when_the_command_commits_events : given.an_occurrence_command_executor
{
    CorrelationId _correlation;
    CorrelationId _ambientCorrelation;

    void Establish()
    {
        CreateExecutor();
        _unitOfWork = NewUnit(2);
        _ambientCorrelation = CorrelationId.New();
        _correlation = _ambientCorrelation;
        var ambient = (ICorrelationIdAccessor)_correlationIdModifier;
        ambient.Current.Returns(_ambientCorrelation);
        _services.GetService(typeof(ICorrelationIdAccessor)).Returns(ambient);
        var current = _ambientCorrelation;
        ambient.Current.Returns(_ => current);
        _correlationIdModifier.When(_ => _.Modify(Arg.Any<CorrelationId>())).Do(_ => current = _.Arg<CorrelationId>());
        _duringExecution = async () =>
        {
            _correlation = ambient.Current;
            OccurrenceExecution.Current!.Enroll(_unitOfWork);
            await _unitOfWork.Commit();
            OccurrenceExecution.Current!.Committed(_unitOfWork);
        };
    }

    async Task Because() => _result = await _executor.Execute(_command, _occurrence, "send-reminder");

    [Fact] void should_report_recorded() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] void should_return_an_appended_outcome() => _result.Outcome.ShouldBeOfExactType<Appended>();
    [Fact] void should_return_the_command_result() => _result.CommandResult.ShouldEqual(_commandResult);
    [Fact] void should_store_a_response_with_the_event_count() => ((int)_result.Response!["events"]!).ShouldEqual(2);
    [Fact] void should_store_the_effect_key() => _unitOfWork.EffectReceipt!.Receipt.Key.ShouldEqual(_key);
    [Fact] async Task should_make_the_receipt_durable() => (await StoredReceipt)!.Outcome.ShouldBeOfExactType<Appended>();
    [Fact] void should_use_a_fresh_correlation_id() => _correlation.ShouldNotEqual(_ambientCorrelation);
    [Fact] void should_not_use_an_unset_correlation_id() => _correlation.ShouldNotEqual(CorrelationId.NotSet);
    [Fact] void should_begin_an_occurrence_causation() => _causationManager.Received(1).BeginScope(OccurrenceCausation.Type, Arg.Is<IDictionary<string, string>>(_ => _["occurrenceId"] == "occurrence-1" && _["effectKey"] == _key.Value && _["attempt"] == "2" && _["epoch"] == "3" && _["kind"] == "Schedule"));
    [Fact] void should_run_as_the_system_without_roles() => _systemExecution.Received(1).AsSystem(Arg.Is<string[]>(_ => _.Length == 0));
    [Fact] void should_not_leave_the_execution_current() => OccurrenceExecution.Current.ShouldBeNull();
    [Fact] void should_restore_the_previous_correlation_id() => ((ICorrelationIdAccessor)_correlationIdModifier).Current.ShouldEqual(_ambientCorrelation);
}
