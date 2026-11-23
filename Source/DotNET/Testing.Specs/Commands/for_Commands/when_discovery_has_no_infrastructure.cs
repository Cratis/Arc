// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_discovery_has_no_infrastructure : given.a_wiring_check
{
    void Establish() => Services.Clear();
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(NullableCommand)]);
    [Fact] void should_not_treat_missing_infrastructure_as_no_validators() => Problems.Single().Category.ShouldEqual("discovery");
}
