// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_validator_dependency_is_missing : given.a_wiring_check
{
    void Establish() => Types.FindMultiple(typeof(IDiscoverableValidator<>)).Returns([typeof(Validator)]);
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(ValidCommand)]);
    [Fact] void should_report_both_handle_and_constructor() => Problems.Count.ShouldEqual(2);
    [Fact] void should_name_the_validator() => Problems.Any(_ => _.Member.Contains(typeof(Validator).FullName!)).ShouldBeTrue();
}
