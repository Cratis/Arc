// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_chained_source_changes_before_commit : given.a_racing_chain
{
    Task Because() => Race(source: true);
    [Fact] void should_reject_the_append() => _result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_report_a_changed_read() => _result.ValidationResults.Single().ReasonDetail.ShouldEqual("ReadChanged");
    [Fact] void should_not_commit() => _scenario.AppendedEvents.ShouldBeEmpty();
    [Fact] void should_not_fail_with_an_exception() => _result.HasExceptions.ShouldBeFalse();
}
