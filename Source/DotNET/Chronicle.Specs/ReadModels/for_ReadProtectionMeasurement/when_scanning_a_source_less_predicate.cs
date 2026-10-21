// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_scanning_a_source_less_predicate : given.a_measure_pipeline
{
    EventSequenceNumber[] _locators;

    void Establish()
    {
        _log.GetTailSequenceNumber(null, null, null, null, Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)105ul);
        ((given.MeasuredEventLog)_log).GetMetadataAt(Arg.Any<IEnumerable<EventSequenceNumber>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _locators = call.ArgAt<IEnumerable<EventSequenceNumber>>(0).ToArray();
                return ImmutableDictionary<EventSequenceNumber, EventMetadata>.Empty.Add(101ul, Metadata(101, "different-source"));
            });
    }

    async Task Because() => await new ReadProtectionMeasureScan(GetType(), _log, 100ul, [new(null, null, [_eventType], null, null, null)], [], 2, TimeSpan.FromSeconds(5), null).Run();

    [Fact] void should_query_the_tail_without_source_narrowing() => _log.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == "GetTailSequenceNumber").ShouldEachConformTo(_ => _.GetArguments()[0] is null);
    [Fact] void should_report_a_matching_other_source() => _telemetry.Select(_ => _.Category).ShouldContain("matching_events_since_start");
    [Fact] void should_bound_server_and_client_scan_volume() => _locators.Select(_ => _.Value).ShouldEqual(new ulong[] { 101, 102 });
    [Fact] void should_report_truncation_as_unavailable() => _telemetry.ShouldContain(_ => _.Category == "measurement_unavailable" && _.Reason!.Contains("Scan volume limit", StringComparison.Ordinal));
}
