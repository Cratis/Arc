// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_duplicate_endpoint_rules_are_configured : given.a_capability_startup_validation
{
    void Establish() => _capabilities.GatedEndpoints = new CapabilityEndpointSelector().ForRoutePattern("/mcp").ForRoutePattern("/MCP");
    Task Because() => Validate();
    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<CapabilityEndpointSelectorIsAmbiguous>();
}
