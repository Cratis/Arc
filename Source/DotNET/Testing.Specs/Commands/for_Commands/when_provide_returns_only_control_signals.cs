// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_provide_returns_only_control_signals : given.a_wiring_check
{
    void Establish() => Services.AddScoped<IDependency, Dependency>();
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(given.handle_bindings.ControlCommand)]);
    [Fact] void should_still_check_the_ordinary_di_binding() => Problems.ShouldBeEmpty();
}
