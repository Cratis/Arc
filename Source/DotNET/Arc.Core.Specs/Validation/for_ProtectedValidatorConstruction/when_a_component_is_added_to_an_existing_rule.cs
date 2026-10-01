// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation.for_DiscoverableValidators;
using FluentValidation;
using FluentValidation.Validators;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction;

public class when_a_component_is_added_to_an_existing_rule : Specification
{
    CommandWithoutDependenciesValidator _validator = null!;
    bool _certifiedAsConstructed;
    bool _certifiedAfterAddingAComponent;

    void Because()
    {
        _validator = (CommandWithoutDependenciesValidator)ProtectedValidatorConstruction.Construct(typeof(CommandWithoutDependenciesValidator));
        _certifiedAsConstructed = ProtectedValidatorConstruction.IsCertified(_validator, typeof(CommandWithoutDependenciesValidator));
        var rule = (IValidationRule<CommandWithoutDependencies, string>)_validator.First();
        rule.AddValidator(new NotNullValidator<CommandWithoutDependencies, string>());
        _certifiedAfterAddingAComponent = ProtectedValidatorConstruction.IsCertified(_validator, typeof(CommandWithoutDependenciesValidator));
    }

    [Fact] void should_certify_it_as_constructed() => _certifiedAsConstructed.ShouldBeTrue();
    [Fact] void should_not_certify_it_once_a_component_is_added() => _certifiedAfterAddingAComponent.ShouldBeFalse();
}
