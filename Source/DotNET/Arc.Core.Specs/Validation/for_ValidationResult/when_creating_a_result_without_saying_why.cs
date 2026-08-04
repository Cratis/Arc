// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ValidationResult;

/// <summary>
/// The default has to be the authored-rule case. Every rejection an application composes goes through these
/// factories without naming a reason, and calling those something the framework composed would invert the meaning
/// of the discriminator for the overwhelmingly common case.
/// </summary>
public class when_creating_a_result_without_saying_why : Specification
{
    [Fact] void should_treat_an_error_as_an_authored_rule() => ValidationResult.Error("nope").Reason.ShouldEqual(ValidationResultReason.Rule);
    [Fact] void should_treat_a_warning_as_an_authored_rule() => ValidationResult.Warning("careful").Reason.ShouldEqual(ValidationResultReason.Rule);
    [Fact] void should_treat_information_as_an_authored_rule() => ValidationResult.Information("hello").Reason.ShouldEqual(ValidationResultReason.Rule);
    [Fact] void should_treat_positional_construction_as_an_authored_rule() => new ValidationResult(ValidationResultSeverity.Error, "nope", [], null!).Reason.ShouldEqual(ValidationResultReason.Rule);
    [Fact] void should_leave_the_authors_state_alone() => ValidationResult.Error("nope", state: "mine").State.ShouldEqual("mine");
}
