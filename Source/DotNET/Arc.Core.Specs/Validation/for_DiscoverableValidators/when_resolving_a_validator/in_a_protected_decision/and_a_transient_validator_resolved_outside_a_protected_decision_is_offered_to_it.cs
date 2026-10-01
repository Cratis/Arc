// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_a_transient_validator_resolved_outside_a_protected_decision_is_offered_to_it : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;

    void Establish()
    {
        var bound = new ServiceCollection().AddTransient<CommandWithoutDependenciesValidator>();
        bound.ConstructDependencyFreeValidatorsThroughArc(Cratis.Types.Types.Instance);
        using var boundProvider = bound.BuildServiceProvider();

        // A caching provider hands the instance it resolved earlier, outside any protected decision, to the protected one.
        var cached = boundProvider.GetRequiredService<CommandWithoutDependenciesValidator>();
        _services.AddSingleton(cached);
    }

    void Because() => _result = ResolveInProtectedDecision(typeof(CommandWithoutDependencies));

    [Fact] void should_refuse_it() => _result.Error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    [Fact] void should_not_return_a_validator() => _result.Validator.ShouldBeNull();
}
