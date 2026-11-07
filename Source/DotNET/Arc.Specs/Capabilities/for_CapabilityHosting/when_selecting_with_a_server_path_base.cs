// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_selecting_with_a_server_path_base : given.a_selective_capability_host
{
    int _selected;
    int _withoutBase;
    int _legacy;

    protected override bool ServerPathBase => true;

    async Task Because()
    {
        _selected = await Request("/base/mcp-spec");
        _withoutBase = await Request("/mcp-spec");
        _legacy = await Request("/base/legacy/ping");
    }

    [Fact] void should_gate_the_based_endpoint() => _selected.ShouldEqual(401);
    [Fact] void should_gate_requests_without_the_base() => _withoutBase.ShouldEqual(401);
    [Fact] void should_recognize_based_legacy_routes() => _legacy.ShouldEqual(200);
    [Fact] void should_not_reach_the_selected_handler() => _selectedCalls.ShouldEqual(0);
}
