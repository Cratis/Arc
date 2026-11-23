// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_provide_may_supply_handle_arguments : given.a_wiring_check
{
    void Establish() => Services.AddScoped<IDependency, Dependency>();
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, new CommandWiringOptions { Rules = [new given.handle_bindings.RejectingRule()] }, [typeof(given.handle_bindings.ProvidedCommand), typeof(given.handle_bindings.UnionProvidedCommand), typeof(given.handle_bindings.OpaqueProvidedCommand)]);
    [Fact] void should_require_invocation_evidence_for_all_possible_shapes() => Problems.Count.ShouldEqual(3);
    [Fact] void should_not_approve_opaque_union_values() => Problems.Single(_ => _.CommandType == typeof(given.handle_bindings.OpaqueProvidedCommand)).Category.ShouldEqual("context-required");
    [Fact] void should_not_guess_provided_values_or_fall_through_to_resolvers_and_di() => Problems.All(_ => _.Category == "context-required").ShouldBeTrue();
}
