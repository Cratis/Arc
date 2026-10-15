// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_a_stream_decision_displaces_a_read : given.a_measure_pipeline
{
    CommandResult _off;
    CommandResult _measure;
    readonly List<ConcurrencyScope> _scopes = [];

    void Establish()
    {
        _log.GetTailSequenceNumber("subject", null, "approval", "month").Returns((EventSequenceNumber)90ul);
        _log.GetForEventSourceIdAndEventTypes("subject", Arg.Any<IEnumerable<EventType>>(), "approval", "month", null).Returns(ImmutableList<AppendedEvent>.Empty);
    }

    async Task Because()
    {
        async Task Handle(CommandContext context)
        {
            ReadParameter(context, "stock");
            var accessor = Substitute.For<ICommandContextAccessor>();
            accessor.Current.Returns(context);
            var decision = await new StreamReads(_log, accessor).Stream("subject", new(EventSourceType.Default, "approval", "month"));
            _scopes.Add(decision.ConcurrencyScope);
            Stage(context);
        }
        _off = await Run(false, Handle);
        _measure = await Run(true, Handle);
    }

    [Fact] void should_keep_the_scope() => _scopes[1].ShouldEqual(_scopes[0]);
    [Fact] void should_keep_result_and_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events_and_conditions() => _batches[1].ShouldEqual(_batches[0]);
    [Fact] void should_report_the_displacement_hypothesis() => _telemetry.ShouldContain(_ => _.Category == "scope_refusal" && _.Reason!.Contains("Stream decision", StringComparison.Ordinal));
}
