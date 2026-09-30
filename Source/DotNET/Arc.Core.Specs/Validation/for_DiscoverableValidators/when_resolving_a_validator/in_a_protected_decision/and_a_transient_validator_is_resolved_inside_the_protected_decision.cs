// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_a_transient_validator_is_resolved_inside_the_protected_decision : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;

    void Establish()
    {
        _services.AddTransient<CommandWithoutDependenciesValidator>();
        _services.ConstructDependencyFreeValidatorsThroughArc(Cratis.Types.Types.Instance);
    }

    void Because() => _result = ResolveInProtectedDecision(typeof(CommandWithoutDependencies));

    [Fact] void should_not_refuse_it() => _result.Error.ShouldBeNull();
    [Fact] void should_return_a_certified_validator() => ProtectedValidatorConstruction.IsCertified(_result.Validator!, typeof(CommandWithoutDependenciesValidator)).ShouldBeTrue();
}
