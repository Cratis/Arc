// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_the_background_scan_fails : given.a_measure_pipeline
{
    Exception _error;

    void Establish() => _log.GetTailSequenceNumber("subject", null, null, null, Arg.Any<IEnumerable<EventType>>()).Returns(Task.FromException<EventSequenceNumber>(new InvalidOperationException("Scan failure")));

    async Task Because() => _error = await Catch.Exception(() => new ReadProtectionMeasureScan(GetType(), _log, 100ul, [new(typeof(Stock), "subject", [_eventType], null, null, null)], [], 500, TimeSpan.FromSeconds(5), null).Run());

    [Fact] void should_not_escape_the_worker() => _error.ShouldBeNull();
    [Fact] void should_report_unavailable() => _telemetry.ShouldContain(_ => _.Category == "measurement_unavailable" && _.Reason == nameof(InvalidOperationException));
}
