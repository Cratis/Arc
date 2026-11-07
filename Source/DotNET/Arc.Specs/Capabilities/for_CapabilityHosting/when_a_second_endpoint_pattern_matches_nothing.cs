// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_a_second_endpoint_pattern_matches_nothing : given.a_selective_capability_host
{
    protected override CapabilityEndpointSelector Selector => new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec").ForRoutePattern("/mcp-sepc");
    protected override bool CaptureStartupError => true;

    [Fact] void should_refuse_startup_even_when_the_first_pattern_matches() => _startupError.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
}
