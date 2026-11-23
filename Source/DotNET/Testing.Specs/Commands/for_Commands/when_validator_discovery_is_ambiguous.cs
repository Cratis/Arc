// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_validator_discovery_is_ambiguous : given.a_wiring_check
{
    void Establish() => Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(Validator), typeof(Validator)]);
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(NullableCommand)]);
    [Fact] void should_fail_for_the_application_universe() => Problems.Single().Category.ShouldEqual("discovery");
    [Fact] void should_preserve_the_duplicate_validator_failure() => Problems.Single().Cause.ShouldNotBeNull();
}
