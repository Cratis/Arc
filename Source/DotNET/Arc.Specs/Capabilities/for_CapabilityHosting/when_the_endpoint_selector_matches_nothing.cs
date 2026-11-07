// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_the_endpoint_selector_matches_nothing : given.a_selective_capability_host
{
    protected override CapabilityEndpointSelector Selector => new CapabilityEndpointSelector().ForRoutePattern("/not-registered");
    protected override bool CaptureStartupError => true;

    [Fact] void should_refuse_startup_before_listening() => _startupError.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
    [Fact] void should_not_reach_any_selected_handler() => _selectedCalls.ShouldEqual(0);
}
