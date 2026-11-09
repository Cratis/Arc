// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Mcp.for_CapabilityRequestContext;

public class when_an_admitted_caller_is_labeled : Specification
{
    Dictionary<object, object?> _items;
    VerifiedCaller _verified;
    CapabilityCaller _capability;

    void Establish()
    {
        _items = new();
        _verified = new("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", new("tenant-a"), null, null, "test", new HashSet<string> { "notes:read" }, new HashSet<string>(), null);
        _capability = CapabilityCallerProjection.From(_verified, "Capabilities", "other");
        _items[CapabilityCallers.ItemKey] = _capability;
        _items[VerifiedCallerPrincipal.ItemKey] = VerifiedCallerPrincipal.Create(_verified);
    }

    void Because() => CapabilityRequestContext.SetProtocol(_items, CapabilityProtocol.Mcp);

    [Fact] void should_preserve_the_verified_identity() => CapabilityRequestContext.GetVerifiedCaller(_items).ShouldEqual(_verified);
    [Fact] void should_only_change_the_protocol() => CapabilityRequestContext.GetCaller(_items).ShouldEqual(_capability with { Protocol = CapabilityProtocol.Mcp });
}
