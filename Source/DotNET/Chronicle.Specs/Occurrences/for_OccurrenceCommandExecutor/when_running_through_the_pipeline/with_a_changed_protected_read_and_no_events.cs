// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_a_changed_protected_read_and_no_events : given.a_protected_decision
{
    void Establish() => ChangeReadBeforeCommit();

    async Task Because() => _result = await Execute(new ReadEffect(Append: false));

    [Fact] void should_leave_the_effect_retryable() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Failed);
    [Fact] void should_surface_the_changed_read_on_the_validate_only_commit() => _result.CommandResult!.ValidationResults.ShouldContain(failure => failure.ReasonDetail == "ReadChanged");
    [Fact] void should_read_the_state_before_the_competitor() => _counter.Read!.Instance!.Value.ShouldEqual("before");
    [Fact] async Task should_not_record_a_no_events_receipt() => (await _store.Occurrences.GetEffectReceipt(_key)).ShouldBeNull();
    [Fact] async Task should_keep_only_the_seed_and_competing_events() => (await _store.EventLog.GetFromSequenceNumber(0)).Count.ShouldEqual(2);
}
