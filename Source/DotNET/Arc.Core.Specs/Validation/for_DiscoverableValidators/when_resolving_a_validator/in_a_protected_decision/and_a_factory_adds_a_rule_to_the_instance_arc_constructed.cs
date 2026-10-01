// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_a_factory_adds_a_rule_to_the_instance_arc_constructed : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;

    void Establish() => _services.AddTransient(_ =>
    {
        var validator = (CommandWithoutDependenciesValidator)ProtectedValidatorConstruction.Construct(typeof(CommandWithoutDependenciesValidator));
        validator.AddDenyingRule();
        return validator;
    });

    void Because() => _result = ResolveInProtectedDecision(typeof(CommandWithoutDependencies));

    [Fact] void should_refuse_it() => _result.Error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    [Fact] void should_not_return_a_validator() => _result.Validator.ShouldBeNull();
}
