// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_handle_names_command_state_or_context : given.a_wiring_check
{
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(given.handle_bindings.ContextCommand)]);
    [Fact] void should_not_invent_bindings_the_runtime_does_not_supply() => Problems.Count.ShouldEqual(3);
    [Fact] void should_require_registered_dependencies_for_each() => Problems.All(_ => _.Category == "missing-service").ShouldBeTrue();
}
