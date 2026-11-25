// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_provide_returns_a_generated_value_type : given.a_wiring_check
{
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(given.handle_bindings.ProvidedGeneratedCommand)]);
    [Fact] void should_accept_context_free_generation_without_executing_provide_or_the_factory() => Problems.ShouldBeEmpty();
}
