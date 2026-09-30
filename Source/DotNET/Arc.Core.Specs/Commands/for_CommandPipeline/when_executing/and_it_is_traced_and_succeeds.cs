// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_and_succeeds : given.a_traced_command_pipeline
{
    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    Activity HandleSpan => _telemetry.Span("cratis.arc.command.handle");

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_name_the_command_span_after_the_command() => CommandSpan.DisplayName.ShouldEqual(nameof(RegisterAuthor));
    [Fact] void should_add_the_command_type() => CommandSpan.GetTagItem("cratis.arc.command.type").ShouldEqual(typeof(RegisterAuthor).FullName);
    [Fact] void should_add_the_correlation_id() => CommandSpan.GetTagItem("cratis.correlation_id").ShouldEqual(_correlationId.ToString());
    [Fact] void should_add_the_outcome() => CommandSpan.GetTagItem("cratis.arc.command.outcome").ShouldEqual("success");
    [Fact] void should_leave_the_status_unset() => CommandSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_name_the_handle_span_after_the_command() => HandleSpan.DisplayName.ShouldEqual($"{nameof(RegisterAuthor)}.Handle()");
    [Fact] void should_nest_the_handle_span_in_the_command_span() => HandleSpan.ParentSpanId.ShouldEqual(CommandSpan.SpanId);
    [Fact] void should_record_one_duration() => Durations.Count().ShouldEqual(1);
    [Fact] void should_record_the_duration_for_the_command_type() => Durations.Single().Tags["cratis.arc.command.type"].ShouldEqual(typeof(RegisterAuthor).FullName);
    [Fact] void should_record_the_duration_as_a_success() => Durations.Single().Tags["cratis.arc.command.outcome"].ShouldEqual("success");
    [Fact] void should_count_one_successful_command() => Outcomes.Single().Tags["cratis.arc.command.outcome"].ShouldEqual("success");
    [Fact] void should_not_record_the_payload() => _telemetry.AnyTagContains(SecretName).ShouldBeFalse();
}
