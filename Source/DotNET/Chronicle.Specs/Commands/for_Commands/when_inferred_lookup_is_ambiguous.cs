// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_inferred_lookup_is_ambiguous : given.a_wiring_check
{
    void Establish() => Services.AddSingleton(Substitute.For<ICommandProtectedDecisionSupport>());
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(AmbiguousCommand)]);
    [Fact] void should_reject_ambiguous_key_metadata() => Problems.Single().Category.ShouldEqual("lookup");
}
