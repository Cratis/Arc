// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_the_bindings_are_copied_into_another_provider : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;

    void Establish()
    {
        var original = new ServiceCollection();
        original.AddTransient<CommandWithoutDependenciesValidator>();
        original.ConstructDependencyFreeValidatorsThroughArc(Cratis.Types.Types.Instance);
        foreach (var descriptor in original)
        {
            _services.Add(descriptor);
        }
    }

    void Because() => _result = ResolveInProtectedDecision(typeof(CommandWithoutDependencies));

    [Fact] void should_not_refuse_it() => _result.Error.ShouldBeNull();
    [Fact] void should_construct_the_validator() => _result.Validator.ShouldBeOfExactType<CommandWithoutDependenciesValidator>();
}
