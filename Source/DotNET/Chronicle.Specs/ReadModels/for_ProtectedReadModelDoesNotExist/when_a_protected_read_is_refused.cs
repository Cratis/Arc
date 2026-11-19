// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelDoesNotExist;

public class when_a_protected_read_is_refused : Specification
{
    ProtectedReadModelDoesNotExist _failure;

    void Because() => _failure = new(typeof(Model), "synthetic-private-key#part");

    [Fact] void should_report_only_the_model_name_in_the_exception() => _failure.Message.ShouldEqual("Model does not exist.");
    [Fact] void should_report_only_the_model_name_in_validation() => _failure.ValidationResult.Message.ShouldEqual("Model does not exist.");
    [Fact] void should_not_include_the_key_in_exception_text() => _failure.ToString().ShouldNotContain("synthetic-private-key");
    [Fact] void should_preserve_the_rule_reason() => _failure.ValidationResult.Reason.ShouldEqual(ValidationResultReason.Rule);
    [Fact] void should_preserve_the_error_severity() => _failure.ValidationResult.Severity.ShouldEqual(ValidationResultSeverity.Error);

    record Model;
}
