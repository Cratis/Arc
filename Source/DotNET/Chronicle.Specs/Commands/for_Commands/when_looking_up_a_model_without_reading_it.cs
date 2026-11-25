// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_looking_up_a_model_without_reading_it : given.a_wiring_check
{
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(DefaultCommand), typeof(ExplicitCommand), typeof(CallerCommand)]);
    [Fact] void should_check_metadata_without_model_state_or_command_getters() => Problems.ShouldBeEmpty();
    [Fact] void should_not_read_the_backing_artifacts() => Artifacts.ReceivedCalls().All(_ => _.GetMethodInfo().Name.StartsWith("get_")).ShouldBeTrue();
}
