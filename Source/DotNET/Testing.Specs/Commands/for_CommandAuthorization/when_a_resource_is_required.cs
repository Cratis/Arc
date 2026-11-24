// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_a_resource_is_required : given.an_evaluation
{
    async Task Because() => Result = await CommandAuthorization.For<given.ResourceCommand>(Provider, Principal);
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_explain_the_missing_resource() => Result.ReasonCode.ShouldEqual("resource-required");
    [Fact] void should_not_evaluate() => Behavior.PolicyCalls.ShouldEqual(0);
}
