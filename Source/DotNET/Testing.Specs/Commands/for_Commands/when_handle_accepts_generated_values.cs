// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_handle_accepts_generated_values : given.a_wiring_check
{
    void Establish() => Services.AddSingleton<given.handle_bindings.Generated>(_ => throw new InvalidOperationException("Must not activate DI"));
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, new CommandWiringOptions { Rules = [new given.handle_bindings.RejectingRule()] }, [typeof(given.handle_bindings.GeneratedCommand)]);
    [Fact] void should_check_declarations_without_generating_or_activating_the_value() => Problems.ShouldBeEmpty();
}
