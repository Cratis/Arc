// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_observing_the_off_path : given.a_measure_pipeline
{
    CommandResult _off;
    CommandResult _measure;
    int _providers;

    async Task Because()
    {
        Task Handle(CommandContext context)
        {
            _providers++;
            foreach (var name in new[] { "stock", "joined", "advisory", "stored", "raw", "store", "mongo", "capacity" }) ReadParameter(context, name);
            Stage(context);
            return Task.CompletedTask;
        }
        _off = await Run(false, Handle);
        _measure = await Run(true, Handle);
    }

    [Fact] void should_keep_the_result() => _measure.IsSuccess.ShouldEqual(_off.IsSuccess);
    [Fact] void should_succeed_in_both_modes() => _measure.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_status_inputs() => (_measure.IsAuthorized, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsAuthorized, _off.IsValid, _off.HasExceptions));
    [Fact] void should_append_identical_events_and_scopes() => _batches[1].ShouldEqual(_batches[0]);
    [Fact] void should_not_repeat_the_provider() => _providers.ShouldEqual(2);
    [Fact] void should_not_acquire_a_decision_read() => _reader.Acquisitions.ShouldEqual(0);
    [Fact] void should_report_protected_reads() => _telemetry.Select(_ => _.Category).ShouldContain("protected_read");
    [Fact] void should_report_the_exact_admission_refusal() => _telemetry.ShouldContain(_ => _.Category == "refused_read" && _.Reason == "Hierarchy");
    [Fact] void should_report_advisory_reads() => _telemetry.ShouldContain(_ => _.Category == "advisory_read" && _.Reason == "Prompt context");
    [Fact] void should_report_stored_api_reads() => _telemetry.ShouldContain(_ => _.Category == "refused_read" && _.Reason == "IReadModels");
    [Fact] void should_report_raw_log_reads() => _telemetry.ShouldContain(_ => _.Category == "refused_read" && _.Reason == "IEventLog");
    [Fact] void should_report_raw_store_reads() => _telemetry.ShouldContain(_ => _.Category == "refused_read" && _.Reason == "IEventStore");
    [Fact] void should_report_mongo_reads() => _telemetry.ShouldContain(_ => _.Category == "refused_read" && _.Reason == "IMongoCollection");
    [Fact] void should_report_services_as_unknown() => _telemetry.Select(_ => _.Category).ShouldContain("unknown_dependency");
    [Fact] void should_report_displacement() => _telemetry.Select(_ => _.Category).ShouldContain("displacement");
    [Fact] void should_not_claim_a_covering_scope_would_be_refused() => _telemetry.Select(_ => _.Category).ShouldNotContain("scope_refusal");
    [Fact] void should_report_self_writes() => _telemetry.Select(_ => _.Category).ShouldContain("self_write");
    [Fact] void should_not_count_the_real_commit_as_immediate() => _telemetry.Select(_ => _.Category).ShouldNotContain("immediate_append");
}
