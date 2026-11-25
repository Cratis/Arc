// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_private_handle_dependency_is_nullable : given.a_wiring_check
{
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(NullableCommand)]);
    [Fact] void should_allow_an_unregistered_nullable_dependency() => Problems.ShouldBeEmpty();
}
