// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_a_changed_protected_read_and_events : given.a_protected_decision
{
    void Establish() => ChangeReadBeforeCommit();

    async Task Because() => _result = await Execute(new ReadEffect(Append: true));

    [Fact] void should_leave_the_effect_retryable() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Failed);
    [Fact] void should_surface_the_changed_read() => _result.CommandResult!.ValidationResults.ShouldContain(failure => failure.ReasonDetail == "ReadChanged");
    [Fact] void should_read_the_state_before_the_competitor() => _counter.Read!.Instance!.Value.ShouldEqual("before");
    [Fact] async Task should_not_append_the_command_event() => (await _store.EventLog.GetFromSequenceNumber(0, "effect-source")).ShouldBeEmpty();
    [Fact] async Task should_not_record_a_receipt() => (await _store.Occurrences.GetEffectReceipt(_key)).ShouldBeNull();
    [Fact] async Task should_keep_the_competing_event() => (await _store.EventLog.GetFromSequenceNumber(0, "read-source")).Count.ShouldEqual(2);
}
