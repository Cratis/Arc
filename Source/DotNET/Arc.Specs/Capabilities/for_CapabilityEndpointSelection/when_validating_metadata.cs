// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using static Cratis.Arc.Capabilities.for_CapabilityEndpointSelection.given.endpoints;

namespace Cratis.Arc.Capabilities.for_CapabilityEndpointSelection;

public class when_validating_metadata : Specification
{
    Exception? _duplicate;
    Exception? _overlap;

    void Because()
    {
        var duplicate = new CapabilityEndpointSelection(Options.Create(new CapabilitiesOptions
        {
            GatedEndpoints = new CapabilityEndpointSelector().ForEndpointMetadata<Marker>().ForEndpointMetadata<Marker>()
        }));
        var overlap = new CapabilityEndpointSelection(Options.Create(new CapabilitiesOptions
        {
            GatedEndpoints = new CapabilityEndpointSelector().ForEndpointMetadata<Marker>().ForEndpointMetadata<DerivedMarker>()
        }));
        _duplicate = Catch.Exception(duplicate.Validate);
        _overlap = Catch.Exception(overlap.Validate);
    }

    [Fact] void should_refuse_duplicate_types() => _duplicate.ShouldBeOfExactType<CapabilityEndpointSelectorIsAmbiguous>();
    [Fact] void should_refuse_assignable_types() => _overlap.ShouldBeOfExactType<CapabilityEndpointSelectorIsAmbiguous>();
}
