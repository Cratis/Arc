// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_runtime_supplies_an_unregistered_token : given.a_wiring_check
{
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(given.handle_bindings.CancellationCommand)]);
    [Fact] void should_not_require_a_cancellation_token_registration() => Problems.ShouldBeEmpty();
}
