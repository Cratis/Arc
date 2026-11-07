// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_selecting_with_a_late_path_base : given.a_selective_capability_host
{
    int _selected;
    int _legacy;

    protected override bool LatePathBase => true;

    async Task Because()
    {
        _selected = await Request("/base/mcp-spec");
        _legacy = await Request("/base/legacy/ping");
    }

    [Fact] void should_gate_the_unclassified_selected_request() => _selected.ShouldEqual(401);
    [Fact] void should_gate_the_unclassified_legacy_request() => _legacy.ShouldEqual(401);
    [Fact] void should_not_reach_the_selected_handler() => _selectedCalls.ShouldEqual(0);
}
