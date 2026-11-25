// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_a_validator_requires_contextual_state : given.a_wiring_check
{
    void Establish() => Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(Validator)]);
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(DefaultCommand)]);
    [Fact] void should_require_an_explicit_constructor_fixture() => Problems.Single().Category.ShouldEqual("context-required");
    [Fact] void should_name_the_validator() => Problems.Single().Member.ShouldContain("Validator");
}
