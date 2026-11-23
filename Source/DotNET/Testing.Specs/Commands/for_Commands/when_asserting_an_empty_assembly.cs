// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_asserting_an_empty_assembly : given.a_wiring_check
{
    CommandsAreNotExecutable _failure;
    void Because() => _failure = Catch.Exception(() => CommandWiring.ShouldBeExecutableIn(Provider, typeof(string).Assembly)) as CommandsAreNotExecutable;
    [Fact] void should_fail_instead_of_passing_vacuously() => _failure.ShouldNotBeNull();
    [Fact] void should_identify_discovery() => _failure.Problems.All(_ => _.Category == "discovery").ShouldBeTrue();
}
