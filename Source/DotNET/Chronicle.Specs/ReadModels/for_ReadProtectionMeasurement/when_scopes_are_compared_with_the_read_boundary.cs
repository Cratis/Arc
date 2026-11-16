// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_scopes_are_compared_with_the_read_boundary : given.a_measure_pipeline
{
    CommandResult _result;

    async Task Because()
    {
        _result = await Run(true, context =>
        {
            ReadParameter(context, "stock");
            var measurement = ReadProtectionMeasurement.Current!;
            measurement.Scope("subject", new ConcurrencyScope(100ul, (EventSourceId)"subject"), "equal");
            measurement.Scope("subject", new ConcurrencyScope(101ul, (EventSourceId)"subject"), "later");
            measurement.Scope("subject", new ConcurrencyScope(90ul, (EventSourceId)"subject", EventStreamType: "approval"), "stream_type");
            measurement.Scope("subject", new ConcurrencyScope(90ul, (EventSourceId)"subject", EventStreamId: "month"), "stream_id");
            measurement.Scope("subject", new ConcurrencyScope(90ul, (EventSourceId)"subject", EventSourceType: "account"), "source_type");
            measurement.Scope("subject", ConcurrencyScope.None, "none");
            measurement.Scope("subject", ConcurrencyScope.NotSet, "not_set");
            Stage(context);
            return Task.CompletedTask;
        });
    }

    [Fact] void should_keep_the_command_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_refuse_a_scope_later_than_the_boundary() => Refused("later").ShouldBeTrue();
    [Fact] void should_refuse_a_narrower_stream_type() => Refused("stream_type").ShouldBeTrue();
    [Fact] void should_refuse_a_narrower_stream_id() => Refused("stream_id").ShouldBeTrue();
    [Fact] void should_refuse_a_narrower_source_type() => Refused("source_type").ShouldBeTrue();
    [Fact] void should_refuse_none() => Refused("none").ShouldBeTrue();
    [Fact] void should_refuse_not_set() => Refused("not_set").ShouldBeTrue();
    [Fact] void should_accept_a_scope_at_the_boundary() => Refused("equal").ShouldBeFalse();

    bool Refused(string reason) => _telemetry.Any(_ => _.Category == "scope_refusal" && _.Reason!.StartsWith($"{reason} is ", StringComparison.Ordinal));
}
