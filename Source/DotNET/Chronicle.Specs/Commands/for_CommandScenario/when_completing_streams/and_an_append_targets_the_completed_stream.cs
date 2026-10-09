// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

public class and_an_append_targets_the_completed_stream : Specification
{
    CommandScenario<AppendToPeriod> _scenario;
    CommandResult _result;

    async Task Establish()
    {
        _scenario = new();
        await _scenario.EventLog.CompleteStream("completion", "period");
    }

    async Task Because() => _result = await _scenario.Execute(new(EventSourceId.New()));

    [Fact] void should_refuse_as_validation() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_report_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_name_the_closed_stream() => _result.ValidationResults.Single().Message.ShouldContain("completion/period");
    [Fact] void should_report_a_constraint_violation() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConstraintViolation);
    void Destroy() => _scenario.Dispose();
}
