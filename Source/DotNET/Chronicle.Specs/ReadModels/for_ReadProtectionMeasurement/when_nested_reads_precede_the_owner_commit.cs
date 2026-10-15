// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_nested_reads_precede_the_owner_commit : given.a_measure_pipeline
{
    CommandResult _off;
    CommandResult _measure;
    int _depth;

    async Task Because()
    {
        async Task Handle(CommandContext context)
        {
            if (_depth++ == 0)
            {
                var nested = await Execute(Activator.CreateInstance(MeasuredCommandType));
                nested.IsSuccess.ShouldBeTrue();
            }
            else
            {
                ReadParameter(context, "stock");
                Stage(context);
            }
            _depth--;
        }
        _off = await Run(false, Handle);
        _measure = await Run(true, Handle);
    }

    [Fact] void should_keep_result_and_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events_and_conditions() => _batches[1].ShouldEqual(_batches[0]);
    [Fact] void should_not_infer_a_nested_position_snapshot_before_commit() => _telemetry.ShouldContain(_ => _.Category == "measurement_unavailable" && _.Reason == "Nested invocation has no committed position snapshot");
    [Fact] void should_still_classify_the_nested_read() => _telemetry.Select(_ => _.Category).ShouldContain("protected_read");
}
