// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_gated_endpoints_are_configured : a_startup_validation
{
    void Because() => Validate();

    [Fact] void should_start() => _error.ShouldBeNull();
    [Fact] void should_keep_the_explicit_selection() => _capabilities.GatedEndpoints.ShouldNotBeNull();
    [Fact] void should_add_the_mcp_endpoint_to_the_selection() => _capabilities.GatedEndpoints!.HasEndpointMetadata<McpEndpointMetadata>().ShouldBeTrue();
    [Fact] void should_not_change_how_the_gate_is_placed() => _capabilities.GateAfterRouting.ShouldBeFalse();
}
