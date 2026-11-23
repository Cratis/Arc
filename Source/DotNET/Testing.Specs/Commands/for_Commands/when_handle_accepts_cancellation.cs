// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_handle_accepts_cancellation : given.a_wiring_check
{
    void Establish() => Services.AddSingleton(typeof(CancellationToken), _ => throw new InvalidOperationException("Must not activate DI"));
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, new CommandWiringOptions { Rules = [new given.handle_bindings.RejectingRule()] }, [typeof(given.handle_bindings.CancellationCommand)]);
    [Fact] void should_use_the_runtime_binding_before_provide_resolvers_and_di() => Problems.ShouldBeEmpty();
}
