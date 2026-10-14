// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_appending_concurrently;

public class with_prior_legacy_seeds : given.a_concurrent_append_scenario
{
    void Establish()
    {
        _scenario.EventScenario.Given.ForEventSource(_source).Events(new FactRecorded("seed"));
        _scenario.AppendConcurrently(_source, new FactRecorded("competitor"));
    }
    async Task Because() => _result = await _scenario.Execute(new AppendWithExplicitScope(_source));

    [Fact] void should_reject_the_commit_after_the_read_boundary() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] async Task should_preserve_the_seed_and_competitor() => (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, _source)).Select(_ => _.Content).ShouldEqual([new FactRecorded("seed"), new FactRecorded("competitor")]);
}
