// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_provide_may_replace_generated_values : given.a_wiring_check
{
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(given.handle_bindings.ProvidedGeneratedCommand)]);
    [Fact] void should_not_approve_a_possible_runtime_refusal_without_invocation_evidence() => Problems.Single().Category.ShouldEqual("context-required");
}
