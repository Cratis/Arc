// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_selecting_endpoint_metadata_over_http : given.a_selective_capability_host
{
    int _alias;
    int _selected;
    int _legacy;

    protected override CapabilityEndpointSelector Selector => new CapabilityEndpointSelector().ForEndpointMetadata<GatedMarker>();
    protected override bool ExplicitRouting => true;

    async Task Because()
    {
        _alias = await Request("/alias/mcp");
        _selected = await Request("/mcp-spec");
        _legacy = await Request("/legacy/ping");
    }

    [Fact] void should_gate_the_alias() => _alias.ShouldEqual(401);
    [Fact] void should_gate_the_selected_endpoint_with_explicit_routing() => _selected.ShouldEqual(401);
    [Fact] void should_leave_legacy_routes_ungated() => _legacy.ShouldEqual(200);
    [Fact] void should_not_reach_the_selected_handler() => _selectedCalls.ShouldEqual(0);
}
