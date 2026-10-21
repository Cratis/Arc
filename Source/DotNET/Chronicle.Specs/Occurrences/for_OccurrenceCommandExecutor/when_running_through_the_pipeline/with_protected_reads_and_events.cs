// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Arc.Chronicle.Occurrences.for_OccurrenceCommandExecutor.when_running_through_the_pipeline;

public class with_protected_reads_and_events : given.a_protected_decision
{
    async Task Because() => _result = await Execute(new ReadEffect(Append: true));

    [Fact] void should_record_the_effect() => _result.Disposition.ShouldEqual(OccurrenceCommandDisposition.Recorded);
    [Fact] void should_report_appended() => _result.Outcome.ShouldBeOfExactType<Appended>();
    [Fact] void should_read_the_existing_state() => _counter.Read!.Instance!.Value.ShouldEqual("before");
    [Fact] void should_acquire_a_protected_read() => _counter.Read!.IsProtected.ShouldBeTrue();
    [Fact] void should_validate_the_read_at_commit() => _counter.Unit!.GetAppendConditions().ShouldContain(condition => condition.Label == "read-source");
    [Fact] async Task should_append_the_command_event() => (await _store.EventLog.GetFromSequenceNumber(0, "effect-source")).Single().Content.ShouldEqual(new EffectHappened("protected"));
    [Fact] void should_record_the_receipt_atomically() => _receiptAtAppend!.Outcome.ShouldBeOfExactType<Appended>();
}
