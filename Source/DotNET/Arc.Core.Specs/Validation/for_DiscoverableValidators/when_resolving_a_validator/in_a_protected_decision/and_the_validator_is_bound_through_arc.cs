// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_the_validator_is_bound_through_arc : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;
    object _registered = null!;

    void Establish()
    {
        _services.AddSingleton<CommandWithoutDependenciesValidator>();
        _services.ConstructDependencyFreeValidatorsThroughArc(Cratis.Types.Types.Instance);
        _provider = _services.BuildServiceProvider();
        _registered = _provider.GetRequiredService<CommandWithoutDependenciesValidator>();
    }

    void Because() => _result = ResolveInProtectedDecision(typeof(CommandWithoutDependencies));

    [Fact] void should_not_refuse_it() => _result.Error.ShouldBeNull();
    [Fact] void should_use_the_registered_instance() => _result.Validator.ShouldEqual(_registered);
}
