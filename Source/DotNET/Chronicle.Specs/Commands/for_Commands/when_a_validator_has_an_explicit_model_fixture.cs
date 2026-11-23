// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_a_validator_has_an_explicit_model_fixture : given.a_wiring_check
{
    void Establish()
    {
        Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(Validator)]);
        Services.AddSingleton(new Model("fixture"));
    }
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(DefaultCommand)]);
    [Fact] void should_construct_the_validator_without_invocation_state() => Problems.ShouldBeEmpty();
}
