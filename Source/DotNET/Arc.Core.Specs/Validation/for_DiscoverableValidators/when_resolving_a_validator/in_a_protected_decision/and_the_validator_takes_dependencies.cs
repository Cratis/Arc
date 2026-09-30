// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_the_validator_takes_dependencies : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;
    int _dependenciesResolved;
    int _validatorsConstructed;

    void Establish()
    {
        _services.AddTransient(_ =>
        {
            _dependenciesResolved++;
            return new CommandDependency { IsAllowed = true };
        });
        _services.AddTransient(services =>
        {
            _validatorsConstructed++;
            return new CommandWithDependencyValidator(services.GetRequiredService<CommandDependency>());
        });
    }

    void Because() => _result = ResolveInProtectedDecision(typeof(CommandWithDependency));

    [Fact] void should_refuse_it() => _result.Error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    [Fact] void should_say_its_constructor_takes_dependencies() => _result.Error!.Message.Contains("constructor takes dependencies").ShouldBeTrue();
    [Fact] void should_advise_moving_the_rule_into_a_decision_read() => _result.Error!.Message.Contains("Provide or Handle as DecisionRead<T>").ShouldBeTrue();
    [Fact] void should_not_resolve_its_dependencies() => _dependenciesResolved.ShouldEqual(0);
    [Fact] void should_not_run_its_registration() => _validatorsConstructed.ShouldEqual(0);
}
