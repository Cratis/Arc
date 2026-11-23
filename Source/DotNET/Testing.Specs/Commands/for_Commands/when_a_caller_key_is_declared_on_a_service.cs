// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_caller_key_is_declared_on_a_service : given.a_wiring_check
{
    void Establish() => Services.AddScoped<IDependency, Dependency>();
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(NonChronicleLookup)]);
    [Fact] void should_reject_even_when_the_service_can_be_resolved() => Problems.Single().Category.ShouldEqual("lookup");
}
