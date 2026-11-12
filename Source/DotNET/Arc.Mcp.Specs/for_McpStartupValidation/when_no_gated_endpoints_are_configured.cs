// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_no_gated_endpoints_are_configured : a_startup_validation
{
    void Establish() => _withGatedEndpoints = false;

    void Because() => Validate();

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_gate_after_routing() => _capabilities.GateAfterRouting.ShouldBeTrue();
    [Fact] void should_not_invent_a_selection() => _capabilities.GatedEndpoints.ShouldBeNull();
}
