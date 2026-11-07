// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_an_empty_endpoint_selector_is_configured : given.a_capability_startup_validation
{
    void Establish() => _capabilities.GatedEndpoints = new CapabilityEndpointSelector();
    Task Because() => Validate();
    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
}
