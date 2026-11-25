// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_context_has_an_old_verdict : given.an_evaluation
{
    async Task Because()
    {
        var context = new CommandContext(CorrelationId.New(), typeof(given.ResourceCommand), new given.ResourceCommand(true), [], CommandContextValues.Empty) { AuthorizedPrincipal = Principal };
        Result = await CommandAuthorization.ForContext<given.ResourceCommand>(Provider, Principal, context);
    }

    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_the_context() => Result.ReasonCode.ShouldEqual("context-invalid");
    [Fact] void should_not_evaluate_a_stale_resource() => Behavior.PolicyCalls.ShouldEqual(0);
}
