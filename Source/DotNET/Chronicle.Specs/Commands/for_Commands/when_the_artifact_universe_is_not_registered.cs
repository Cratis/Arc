// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_the_artifact_universe_is_not_registered : given.a_wiring_check
{
    void Establish() => Services.Remove(Services.Single(_ => _.ServiceType == typeof(Cratis.Chronicle.IClientArtifactsProvider)));
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(DefaultCommand)]);
    [Fact] void should_not_approve_unknown_backing_artifacts() => Problems.Single().Category.ShouldEqual("discovery");
}
