// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityCallerProjection;

public class when_projecting_a_service_caller
{
    [Theory]
    [InlineData(CallerPrincipalKind.Person, CapabilityPrincipalKind.Person)]
    [InlineData(CallerPrincipalKind.DelegatedActor, CapabilityPrincipalKind.DelegatedActor)]
    [InlineData(CallerPrincipalKind.OrganizationAutomation, CapabilityPrincipalKind.OrganizationAutomation)]
    [InlineData(CallerPrincipalKind.ClusterWorkload, CapabilityPrincipalKind.ClusterWorkload)]
    [InlineData(CallerPrincipalKind.Runner, CapabilityPrincipalKind.Runner)]
    public void should_map_each_kind(int source, CapabilityPrincipalKind expected)
    {
        var caller = new VerifiedCaller("1", (CallerPrincipalKind)source, CallerCredentialKind.MachinePrincipal, new("credential"), null, null, new("service"), null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
        var result = CapabilityCallerProjection.From(caller, "group", "rest");
        result.PrincipalKind.ShouldEqual(expected);
        result.Person.ShouldBeNull();
    }
}
