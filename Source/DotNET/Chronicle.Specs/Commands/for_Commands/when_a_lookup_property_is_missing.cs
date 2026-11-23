// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_a_lookup_property_is_missing : given.a_wiring_check
{
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(InvalidPropertyCommand)]);
    [Fact] void should_reject_even_a_nullable_model() => Problems.Single().Category.ShouldEqual("lookup");
    [Fact] void should_name_the_parameter() => Problems.Single().Parameter.ShouldContain("model");
}
