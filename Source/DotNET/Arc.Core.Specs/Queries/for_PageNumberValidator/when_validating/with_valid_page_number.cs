// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_PageNumberValidator.when_validating;

public class with_valid_page_number : Specification
{
    PageNumberValidator _validator;
    FluentValidation.Results.ValidationResult _result;

    void Establish() => _validator = new PageNumberValidator();

    void Because() => _result = _validator.Validate(new PageNumber(0));

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
