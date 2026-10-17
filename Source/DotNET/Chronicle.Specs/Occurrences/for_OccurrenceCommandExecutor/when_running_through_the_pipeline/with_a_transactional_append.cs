// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_a_transactional_append : given.an_in_process_executor
{
    const string PrivateValue = "jane@example.com";
    OccurrenceCommandResult _duplicate;
    Causation[] _causation;

    async Task Because()
    {
        _result = await Execute(new RecordEffect(PrivateValue));
        Assert.True(_result.CommandResult!.IsSuccess, string.Join(Environment.NewLine, _result.CommandResult.ExceptionMessages));
        _duplicate = await Execute(new RecordEffect("different-value"));
        _causation = [.. (await _store.EventLog.GetFromSequenceNumber(0)).Single().Context.Causation];
    }

    [Fact] void should_record_the_effect() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] void should_report_the_appended_outcome() => _result.Outcome.ShouldBeOfExactType<Appended>();
    [Fact] void should_have_the_receipt_when_the_append_is_observed() => _receiptAtAppend.ShouldNotBeNull();
    [Fact] async Task should_append_exactly_one_event() => (await _store.EventLog.GetFromSequenceNumber(0)).Count.ShouldEqual(1);
    [Fact] void should_skip_the_duplicate_handler() => _counter.Calls.ShouldEqual(1);
    [Fact] void should_return_already_recorded() => _duplicate.Disposition.ShouldEqual(OccurrenceCommandDisposition.AlreadyRecorded);
    [Fact] void should_return_the_stored_response() => _duplicate.Response!.ToJsonString().ShouldEqual(_result.Response!.ToJsonString());
    [Fact] void should_persist_the_occurrence_causation() => _causation.ShouldContain(cause => cause.Type == OccurrenceCausation.Type && cause.Properties["effectKey"] == _key.Value);
    [Fact] void should_persist_the_command_causation() => _causation.ShouldContain(cause => cause.Type == CommandCausation.Type);
    [Fact] void should_put_the_occurrence_before_the_command() => Array.FindIndex(_causation, cause => cause.Type == OccurrenceCausation.Type).ShouldBeLessThan(Array.FindIndex(_causation, cause => cause.Type == CommandCausation.Type));
    [Fact] void should_not_store_command_values() => _result.Response!.ToJsonString().ShouldNotContain(PrivateValue);
    [Fact] void should_return_the_private_value_only_in_the_live_command_result() => ((CommandResult<string>)_result.CommandResult!).Response.ShouldEqual($"private-response:{PrivateValue}");
    [Fact] void should_not_store_response_values() => _result.Response!.ToJsonString().ShouldNotContain("private-response");
}
