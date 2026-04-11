// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_PageSizeValidator.when_validating;

public class with_zero_page_size : Specification
{
    PageSizeValidator _validator;
    FluentValidation.Results.ValidationResult _result;

    void Establish() => _validator = new PageSizeValidator();

    void Because() => _result = _validator.Validate(new PageSize(0));

    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_one_error() => _result.Errors.Count.ShouldEqual(1);
    [Fact] void should_report_the_correct_error_message() => _result.Errors[0].ErrorMessage.ShouldEqual("Page size must be greater than 0");
}
