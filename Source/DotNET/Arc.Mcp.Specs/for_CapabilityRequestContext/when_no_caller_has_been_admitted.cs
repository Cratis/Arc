// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Capabilities;

namespace Cratis.Arc.Mcp.for_CapabilityRequestContext;

public class when_no_caller_has_been_admitted : Specification
{
    Dictionary<object, object?> _items;

    void Establish() => _items = new() { ["User"] = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "synthetic-person")], "untrusted")) };

    void Because() => CapabilityRequestContext.SetProtocol(_items, CapabilityProtocol.Mcp);

    [Fact] void should_not_create_a_capability_caller() => CapabilityRequestContext.GetCaller(_items).ShouldBeNull();
    [Fact] void should_not_trust_an_arbitrary_principal() => CapabilityRequestContext.GetVerifiedCaller(_items).ShouldBeNull();
    [Fact] void should_not_add_authority_to_the_item_bag() => _items.Count.ShouldEqual(1);
}
