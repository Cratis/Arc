// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Capabilities.for_CapabilityCallerProjection;

public class when_projecting_a_person_caller : Specification
{
    CapabilityCaller _result;
    VerifiedCaller _caller;

    void Establish() => _caller = new("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", new TenantId("tenant"), null, null, "test", new HashSet<string> { "read" }, new HashSet<string>(), null);
    void Because() => _result = CapabilityCallerProjection.From(_caller, "capabilities", CapabilityProtocol.Rest);

    [Fact] void should_map_person() => _result.Person.ShouldEqual("person");
    [Fact] void should_map_credential() => _result.CredentialId.ShouldEqual("credential");
    [Fact] void should_map_tenant() => _result.Tenant.ShouldEqual(_caller.Tenant);
    [Fact] void should_map_scopes() => _result.Scopes.ShouldEqual(_caller.Scopes);
    [Fact] void should_have_no_actor() => _result.Actor.ShouldBeNull();
    [Fact] void should_carry_group() => _result.EndpointGroup.ShouldEqual("capabilities");
    [Fact] void should_carry_protocol() => _result.Protocol.ShouldEqual("rest");
}
