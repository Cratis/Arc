// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_generation_needs_invocation_context : given.a_wiring_check
{
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(given.handle_bindings.ContextGenerationCommand)]);
    [Fact] void should_report_the_required_receipt_and_tenant_context() => Problems.Single().Category.ShouldEqual("context-required");
}
