// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_the_validator_takes_no_dependencies_and_is_not_registered : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;
    bool _rejectsEmptyName;

    void Because()
    {
        _result = ResolveInProtectedDecision(typeof(CommandWithoutDependencies));
        _rejectsEmptyName = !((IValidator)_result.Validator!).Validate(new ValidationContext<CommandWithoutDependencies>(new(string.Empty))).IsValid;
    }

    [Fact] void should_not_refuse_it() => _result.Error.ShouldBeNull();
    [Fact] void should_construct_the_validator() => _result.Validator.ShouldBeOfExactType<CommandWithoutDependenciesValidator>();
    [Fact] void should_run_its_rules() => _rejectsEmptyName.ShouldBeTrue();
}
