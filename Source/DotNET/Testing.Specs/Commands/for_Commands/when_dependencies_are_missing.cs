// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_dependencies_are_missing : given.a_wiring_check
{
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(ValidCommand), typeof(OtherMissingCommand), typeof(ValidCommand)]);

    [Fact] void should_aggregate_without_duplicates() => Problems.Count.ShouldEqual(2);
    [Fact] void should_identify_the_category() => Problems.All(_ => _.Category == "missing-service").ShouldBeTrue();
    [Fact] void should_sort_by_command() => Problems[0].CommandType.ShouldEqual(typeof(OtherMissingCommand));
    [Fact] void should_name_the_parameter() => Problems[1].Parameter.ShouldContain("dependency");
}
