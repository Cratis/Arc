// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Validation.for_DiscoverableValidators.when_resolving_a_validator.in_a_protected_decision;

public class and_the_validator_is_registered_with_a_factory : given.a_protected_decision
{
    (object? Validator, Exception? Error) _result;

    void Establish() => _services.AddTransient(_ => new CommandWithoutDependenciesValidator());

    void Because() => _result = ResolveInProtectedDecision(typeof(CommandWithoutDependencies));

    [Fact] void should_refuse_it() => _result.Error.ShouldBeOfExactType<DiscoverableValidatorRefusedInProtectedDecision>();
    [Fact] void should_say_arc_did_not_construct_it() => _result.Error!.Message.Contains("instance Arc did not construct").ShouldBeTrue();
    [Fact] void should_advise_letting_convention_discovery_register_it() => _result.Error!.Message.Contains("let convention discovery register it").ShouldBeTrue();
}
