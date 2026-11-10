// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadIncomplete;

public class when_a_protected_fold_is_incomplete : Specification
{
    ReadIncomplete _failure;

    void Because() => _failure = new(typeof(Model), (ReadModelKey)"synthetic-private-key");

    [Fact] void should_report_only_the_model_name_in_the_exception() =>
        _failure.Message.ShouldEqual("Model could not be read completely. Nothing was written; submit it again.");
    [Fact] void should_use_the_same_validation_message() => _failure.ValidationResult.Message.ShouldEqual(_failure.Message);
    [Fact] void should_not_include_the_key_in_exception_text() => _failure.ToString().ShouldNotContain("synthetic-private-key");
    [Fact] void should_preserve_the_concurrency_reason() => _failure.ValidationResult.Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_preserve_the_incomplete_reason_detail() => _failure.ValidationResult.ReasonDetail.ShouldEqual("ReadIncomplete");

    record Model;
}
