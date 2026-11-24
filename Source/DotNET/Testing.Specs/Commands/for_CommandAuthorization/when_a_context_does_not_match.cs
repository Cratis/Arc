// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_context_does_not_match : given.an_evaluation
{
    async Task Because()
    {
        var context = new CommandContext(CorrelationId.New(), typeof(given.RoleCommand), new given.RoleCommand(), [], CommandContextValues.Empty);
        Result = await CommandAuthorization.ForContext<given.ResourceCommand>(Provider, Principal, context);
    }

    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_the_context() => Result.ReasonCode.ShouldEqual("context-invalid");
}
