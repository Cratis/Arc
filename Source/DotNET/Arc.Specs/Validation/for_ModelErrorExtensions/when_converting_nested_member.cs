// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.Validation.for_ModelErrorExtensions;

public class when_converting_nested_member : Specification
{
    const string Member = "FirstLevel.SecondLevel.TheMember";
    readonly ModelError _modelError = new("Some message");
    ValidationResult _validationError;

    void Because() => _validationError = _modelError.ToValidationResult(Member);

    [Fact] void should_hold_message() => _validationError.Message.ShouldEqual(_modelError.ErrorMessage);
    [Fact] void should_hold_camel_cased_member() => _validationError.Members.First().ShouldEqual("firstLevel.secondLevel.theMember");
}
