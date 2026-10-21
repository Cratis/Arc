// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_reads_would_drop_stream_dimensions : given.a_measure_pipeline
{
    [Theory]
    [InlineData(null, null, "transactions", "october", "subject", true)]
    [InlineData("transactions", "october", null, null, "subject", true)]
    [InlineData("transactions", "october", "transactions", "november", "subject", true)]
    [InlineData("transactions", "october", "approvals", "october", "subject", true)]
    [InlineData("transactions", "october", "transactions", "october", "subject", false)]
    [InlineData(null, null, null, null, "subject", false)]
    [InlineData("transactions", "october", "transactions", "november", "another", false)]
    public async Task should_report_only_same_source_stream_widening(string? firstType, string? firstId, string? secondType, string? secondId, string secondSource, bool coarsened)
    {
        Task Handle(CommandContext context)
        {
            ReadProtectionMeasurement.Current?.DecisionRead<Stock>(_reader, "subject", firstType is null ? null : (EventStreamType)firstType, firstId is null ? null : (EventStreamId)firstId);
            ReadProtectionMeasurement.Current?.DecisionRead<Stock>(_reader, secondSource, secondType is null ? null : (EventStreamType)secondType, secondId is null ? null : (EventStreamId)secondId);
            Stage(context);
            return Task.CompletedTask;
        }
        var off = await Run(false, Handle);
        _telemetry.Select(_ => _.Category).ShouldEqual(["mode_lowered"]);
        var measure = await Run(true, Handle);

        _telemetry.Any(_ => _.Category == "coarsened" && _.Reason == "stream_dims_dropped").ShouldEqual(coarsened);
        (measure.IsSuccess, measure.IsValid, measure.HasExceptions).ShouldEqual((off.IsSuccess, off.IsValid, off.HasExceptions));
        _batches[1].ShouldEqual(_batches[0]);
        _reader.Acquisitions.ShouldEqual(0);
    }
}
