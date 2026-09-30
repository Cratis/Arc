// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction;

public class when_certifying_a_validator_with_many_rules : Specification
{
    CommandWithManyRulesValidator _validator = null!;
    bool _certifiedAsConstructed;
    bool _certifiedAfterAddingARule;

    void Because()
    {
        _validator = (CommandWithManyRulesValidator)ProtectedValidatorConstruction.Construct(typeof(CommandWithManyRulesValidator));
        _certifiedAsConstructed = ProtectedValidatorConstruction.IsCertified(_validator, typeof(CommandWithManyRulesValidator));
        _validator.AddRule();
        _certifiedAfterAddingARule = ProtectedValidatorConstruction.IsCertified(_validator, typeof(CommandWithManyRulesValidator));
    }

    [Fact] void should_certify_it_as_constructed() => _certifiedAsConstructed.ShouldBeTrue();
    [Fact] void should_not_certify_it_once_a_rule_is_added() => _certifiedAfterAddingARule.ShouldBeFalse();
}
