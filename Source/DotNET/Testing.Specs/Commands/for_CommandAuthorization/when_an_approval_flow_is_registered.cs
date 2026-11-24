// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;
using Cratis.Arc.Testing.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_an_approval_flow_is_registered : given.an_evaluation
{
    void Establish()
    {
        // The flow contract is internal. Register it without widening Arc.Core's friend assemblies or resolving a flow.
        var flowType = typeof(RequiresApprovalAttribute).Assembly.GetType("Cratis.Arc.Identity.Approvals.IApprovalFlow", throwOnError: true);
        Services.AddSingleton(flowType, _ => throw new InvalidOperationException("Standalone evaluation must not resolve an approval flow."));
    }

    async Task Because() => Result = await CommandAuthorization.For<given.ApprovalCommand>(Provider, Principal);
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_approval_without_resolving_the_flow() => Result.ReasonCode.ShouldEqual("approval-required");
}
