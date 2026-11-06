// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Chronicle.Capabilities.for_CapabilityCausation;

public class when_getting_properties_for_a_delegated_caller_with_client_and_grant
{
    [Theory]
    [InlineData(false, "declared")]
    [InlineData(true, "authenticated")]
    public void should_record_exact_verified_fields(bool authenticated, string authentication)
    {
        var caller = new CapabilityCaller("capabilities", "rest", CapabilityPrincipalKind.DelegatedActor, "person", new("issuer", "subject"), new("client", authenticated), "credential", "grant", new TenantId("tenant"), new HashSet<string> { "unrecorded" });
        var properties = CapabilityCausation.PropertiesFor(caller);
        properties.Keys.ShouldContainOnly("endpointGroup", "protocol", "principalKind", "person", "actorIssuer", "actorSubject", "clientId", "clientAuthentication", "credentialId", "grantId", "tenant");
        properties["principalKind"].ShouldEqual("delegated-actor");
        properties["actorIssuer"].ShouldEqual("issuer");
        properties["actorSubject"].ShouldEqual("subject");
        properties["clientId"].ShouldEqual("client");
        properties["clientAuthentication"].ShouldEqual(authentication);
        properties["grantId"].ShouldEqual("grant");
        properties["tenant"].ShouldEqual("tenant");
    }

    [Theory]
    [InlineData(CapabilityPrincipalKind.Person, "person")]
    [InlineData(CapabilityPrincipalKind.DelegatedActor, "delegated-actor")]
    [InlineData(CapabilityPrincipalKind.OrganizationAutomation, "organization-automation")]
    [InlineData(CapabilityPrincipalKind.ClusterWorkload, "cluster-workload")]
    [InlineData(CapabilityPrincipalKind.Runner, "runner")]
    public void should_use_locked_kind_spellings(CapabilityPrincipalKind kind, string spelling) =>
        CapabilityCausation.PropertiesFor(new("capabilities", "rest", kind, null, null, null, "credential", null, null, new HashSet<string>()))["principalKind"].ShouldEqual(spelling);
}
