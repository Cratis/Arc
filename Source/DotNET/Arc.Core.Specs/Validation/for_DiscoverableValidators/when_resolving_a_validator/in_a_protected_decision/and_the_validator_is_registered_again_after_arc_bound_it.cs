// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_the_validator_is_registered_again_after_arc_bound_it : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;

    void Establish()
    {
        _services.AddTransient<CommandWithoutDependenciesValidator>();
        _services.ConstructDependencyFreeValidatorsThroughArc(Cratis.Types.Types.Instance);
        _services.AddTransient(_ => new CommandWithoutDependenciesValidator());
        _provider = _services.BuildServiceProvider();
    }

    void Because() => _result = ResolveInProtectedDecision(typeof(CommandWithoutDependencies));

    [Fact] void should_refuse_it() => _result.Error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
}
