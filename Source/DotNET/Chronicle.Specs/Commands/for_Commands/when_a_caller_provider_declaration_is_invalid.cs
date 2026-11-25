// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_a_caller_provider_declaration_is_invalid : given.a_wiring_check
{
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(InvalidCallerCommand)]);
    [Fact] void should_reject_the_provider_shape_without_constructing_it() => Problems.Single().Category.ShouldEqual("lookup");
    [Fact] void should_preserve_the_declaration_failure() => Problems.Single().Cause.ShouldNotBeNull();
}
