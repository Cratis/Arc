// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_description_fails : given.a_measure_pipeline
{
    CommandResult _off;
    CommandResult _measure;

    void Establish() => _reader.FailDescription = true;

    async Task Because()
    {
        Task Handle(CommandContext context)
        {
            ReadParameter(context, "stock");
            Stage(context);
            return Task.CompletedTask;
        }
        _off = await Run(false, Handle);
        _measure = await Run(true, Handle);
    }

    [Fact] void should_report_unavailable() => _telemetry.Select(_ => _.Category).ShouldContain("measurement_unavailable");
    [Fact] void should_succeed() => _measure.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events() => _batches[1].ShouldEqual(_batches[0]);
}
