// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Capabilities.for_CapabilityCallerProjection;

public class when_projecting_a_delegated_caller : Specification
{
    CapabilityCaller _authenticated;
    CapabilityCaller _declared;

    void Because()
    {
        var caller = new VerifiedCaller("1", CallerPrincipalKind.DelegatedActor, CallerCredentialKind.OAuthGrant, new("credential"), null, new("person"), null, new("issuer", "subject"), new("grant"), null, new("client", CallerClientAuthentication.Authenticated), "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);
        _authenticated = CapabilityCallerProjection.From(caller, "group", "rest");
        _declared = CapabilityCallerProjection.From(caller with { Client = new("client", CallerClientAuthentication.Declared) }, "group", "rest");
    }

    [Fact] void should_map_actor() => _authenticated.Actor.ShouldEqual(new CapabilityActor("issuer", "subject"));
    [Fact] void should_map_grant() => _authenticated.GrantId.ShouldEqual("grant");
    [Fact] void should_map_authenticated_client() => _authenticated.Client.ShouldEqual(new CapabilityClient("client", true));
    [Fact] void should_map_declared_client() => _declared.Client.ShouldEqual(new CapabilityClient("client", false));
}
