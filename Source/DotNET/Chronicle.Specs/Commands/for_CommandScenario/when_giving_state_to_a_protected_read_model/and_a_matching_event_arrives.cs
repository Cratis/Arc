// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_giving_state_to_a_protected_read_model;

public class and_a_matching_event_arrives : given.a_pinned_state
{
    void Establish() => _scenario.AppendConcurrently(_source, new StateChanged("competing"));

    async Task Because() => _result = await _scenario.Execute(new(_source), global::Xunit.TestContext.Current.CancellationToken);

    [Fact] void should_not_fail_with_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_report_a_concurrency_violation() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_report_read_changed() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
    [Fact] void should_not_commit_the_owner_event() => _scenario.AppendedEvents.ShouldBeEmpty();
}
